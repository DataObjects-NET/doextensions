using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Synchronization;
using Xtensive.Core;
using Xtensive.Orm.Model;
using Xtensive.Orm.Services;
using Xtensive.Orm.Sync.DataExchange;
using Xtensive.Tuples;
using FieldInfo = Xtensive.Orm.Model.FieldInfo;

namespace Xtensive.Orm.Sync
{
  internal sealed class SyncSession : INotifyingChangeApplierTarget
  {
    private static readonly MethodInfo CreateKeyMethod;

    private readonly Session session;
    private readonly SyncConfiguration configuration;
    private readonly MetadataManager metadataManager;
    private readonly KeyMap keyMap;
    private readonly DirectEntityAccessor accessor;
    private readonly Dictionary<Key, List<KeyDependency>> keyDependencies;
    private readonly EntityTupleFormatterRegistry tupleFormatters;
    private readonly MetadataFetcher metadataFetcher;
    private readonly SyncTickGenerator tickGenerator;
    private readonly SyncSessionContext syncContext;

    private ChangeSet currentChangeSet;
    private IEnumerator<ChangeSet> changeSetEnumerator;

    public SyncIdFormatGroup IdFormats { get { return WellKnown.IdFormats; } }

    public ReplicaState ReplicaState { get { return metadataManager.ReplicaState; } }

    #region Source provider methods

    private bool FilteredBatchIsRequired()
    {
      var c = configuration;
      return c.SyncTypes.Count > 0 || c.Filters.Count > 0 || c.SkipTypes.Count > 0;
    }

    public ChangeBatch GetChangeBatch(uint batchSize, SyncKnowledge destinationKnowledge)
    {
      var result = CreateChangeBatch(destinationKnowledge);

      bool hasNext;

      if (changeSetEnumerator==null) {
        var changeSets = metadataManager.DetectChanges(batchSize, destinationKnowledge);
        changeSetEnumerator = changeSets.GetEnumerator();
        hasNext = changeSetEnumerator.MoveNext();
        if (!hasNext) {
          result.BeginUnorderedGroup();
          result.EndUnorderedGroup(ReplicaState.CurrentKnowledge, true);
          result.SetLastBatch();
          return result;
        }
      }

      result.BeginUnorderedGroup();
      currentChangeSet = changeSetEnumerator.Current;
      result.AddChanges(currentChangeSet.GetItemChanges());

      hasNext = changeSetEnumerator.MoveNext();
      if (!hasNext) {
        result.EndUnorderedGroup(ReplicaState.CurrentKnowledge, true);
        result.SetLastBatch();
      }
      else
        result.EndUnorderedGroup(ReplicaState.CurrentKnowledge, false);

      return result;
    }

    private ChangeBatch CreateChangeBatch(SyncKnowledge destinationKnowledge)
    {
      if (!FilteredBatchIsRequired())
        return new ChangeBatch(IdFormats, destinationKnowledge, ReplicaState.ForgottenKnowledge);
      var filterInfo = new ItemListFilterInfo(IdFormats);
      return new ChangeBatch(IdFormats, destinationKnowledge, ReplicaState.ForgottenKnowledge, filterInfo);
    }

    #endregion

    #region Destination provider methods

    public void ProcessChangeBatch(
      ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges, IChangeDataRetriever changeDataRetriever, SyncCallbacks syncCallbacks)
    {
      var localChanges = new List<ItemChange>();
      metadataManager.GetLocalChanges(sourceChanges, localChanges);

      var knowledge = ReplicaState.CurrentKnowledge;
      var forgottenKnowledge = ReplicaState.ForgottenKnowledge;
      var changeApplier = new NotifyingChangeApplier(IdFormats);

      changeApplier.ApplyChanges(
        resolutionPolicy, sourceChanges, changeDataRetriever,
        localChanges, knowledge, forgottenKnowledge, this, syncContext, syncCallbacks);
    }

    public bool TryGetDestinationVersion(ItemChange sourceChange, out ItemChange destinationVersion)
    {
      throw new NotSupportedException("INotifyingChangeApplierTarget.TryGetDestinationVersion");
    }

    public void SaveItemChange(SaveChangeAction saveChangeAction, ItemChange change, SaveChangeContext context)
    {
      var data = context.ChangeData as ItemChangeData;
      if (data!=null)
        data.Change = change;

      switch (saveChangeAction) {
        case SaveChangeAction.Create:
          HandleCreateEntity(data);
          break;
        case SaveChangeAction.DeleteAndStoreTombstone:
          HandleRemoveEntity(change);
          break;
        case SaveChangeAction.UpdateVersionAndData:
          HandleUpdateEntity(data);
          break;
        default:
          throw new NotSupportedException(string.Format("SaveItemChange({0})", saveChangeAction.ToString()));
      }
    }

    private void HandleCreateEntity(ItemChangeData data)
    {
      var entityType = data.Identity.Key.TypeReference.Type.UnderlyingType;
      var typeInfo = session.Domain.Model.Types[entityType];
      var hierarchy = typeInfo.Hierarchy;
      Key mappedKey = null;
      int offset;

      switch (hierarchy.Key.GeneratorKind) {
        case KeyGeneratorKind.Custom:
        case KeyGeneratorKind.Default:
          mappedKey = Key.Create(session, entityType);
          break;
        case KeyGeneratorKind.None:
          var originalTuple = data.Identity.Key.Value;
          var targetTuple = originalTuple.Clone();
          foreach (var field in hierarchy.Key.Fields.Where(f => f.IsEntity)) {
            Identity identity;
            if (!data.References.TryGetValue(field.Name, out identity))
              continue;
            var mappedRefKey = TryResolveIdentity(identity);
            if (mappedRefKey==null)
              throw new InvalidOperationException(string.Format("Mapped key for original key '{0}' is not found", identity.Key.Format()));
            offset = field.MappingInfo.Offset;
            mappedRefKey.Value.CopyTo(targetTuple, 0, offset, field.MappingInfo.Length);
          }
          mappedKey = (Key) CreateKeyMethod.Invoke(null, new object[] {session.Domain, typeInfo, TypeReferenceAccuracy.ExactType, targetTuple});
          break;
      }

      RegisterKeyMapping(data, mappedKey);
      metadataManager.CreateMetadata(mappedKey, data.Change);
      var entity = accessor.CreateEntity(entityType, mappedKey.Value);
      var state = accessor.GetEntityState(entity);
      offset = mappedKey.Value.Count;
      var changeDataTuple = tupleFormatters.Get(entityType).Parse(data.TupleValue);
      changeDataTuple.CopyTo(state.Tuple, offset, offset, changeDataTuple.Count - offset);
      UpdateReferences(state, data.References);
    }

    private void HandleUpdateEntity(ItemChangeData data)
    {
      var syncInfo = metadataFetcher.GetMetadata(data.Identity.GlobalId);
      if (syncInfo==null)
        return;

      metadataManager.UpdateMetadata(syncInfo, data.Change, false);
      var entity = syncInfo.SyncTarget;
      var state = accessor.GetEntityState(entity);
      var offset = entity.Key.Value.Count;
      var changeDataTuple = tupleFormatters.Get(entity.TypeInfo.UnderlyingType).Parse(data.TupleValue);
      changeDataTuple.CopyTo(state.DifferentialTuple, offset, offset, changeDataTuple.Count - offset);
      state.PersistenceState = PersistenceState.Modified;
      UpdateReferences(state, data.References);
    }

    private void HandleRemoveEntity(ItemChange change)
    {
      var syncInfo = metadataFetcher.GetMetadata(change.ItemId);
      if (syncInfo==null)
        return;

      metadataManager.UpdateMetadata(syncInfo, change, true);
      var entity = syncInfo.SyncTarget;
      var state = accessor.GetEntityState(entity);
      state.PersistenceState = PersistenceState.Removed;
    }

    private void RegisterKeyMapping(ItemChangeData data, Key mappedKey)
    {
      keyMap.Register(data.Identity, mappedKey);
      List<KeyDependency> dependencies;
      if (!keyDependencies.TryGetValue(data.Identity.Key, out dependencies))
        return;

      foreach (var dependency in dependencies)
        accessor.SetReferenceKey(dependency.Target.Entity, dependency.Field, mappedKey);
      keyDependencies.Remove(data.Identity.Key);
    }

    private Key TryResolveIdentity(Identity identity)
    {
      var cachedValue = keyMap.Resolve(identity);
      if (cachedValue!=null)
        return cachedValue;
      
      var syncInfo = metadataFetcher.GetMetadata(identity.GlobalId);
      if (syncInfo!=null) {
        keyMap.Register(identity, syncInfo.SyncTargetKey);
        return syncInfo.SyncTargetKey;
      }

      return null;
    }

    private void UpdateReferences(EntityState state, Dictionary<string, Identity> references)
    {
      var typeInfo = state.Type;
      foreach (var field in typeInfo.Fields.Where(f => f.IsEntity && !f.IsPrimaryKey)) {
        Identity reference;
        if (!references.TryGetValue(field.Name, out reference))
          continue;
        var mappedKey = TryResolveIdentity(reference);
        if (mappedKey==null) {
          RegisterReferenceDependency(state, field, reference);
        }
        else
          //mappedKey.Value.CopyTo(state.Tuple, field.MappingInfo.Offset);
          accessor.SetReferenceKey(state.Entity, field, mappedKey);
      }
    }

    private void RegisterReferenceDependency(EntityState state, FieldInfo field, Identity value)
    {
      List<KeyDependency> container;
      if (!keyDependencies.TryGetValue(value.Key, out container)) {
        container = new List<KeyDependency>();
        keyDependencies[value.Key] = container;
      }
      container.Add(new KeyDependency(state, field, value));
    }

    public ulong GetNextTickCount()
    {
      return (ulong) tickGenerator.GetNextTick();
    }

    public IChangeDataRetriever GetDataRetriever()
    {
      return new ChangeDataRetriever(IdFormats, currentChangeSet);
    }

    public void StoreKnowledgeForScope(SyncKnowledge currentKnowledge, ForgottenKnowledge forgottenKnowledge)
    {
      ReplicaState.CurrentKnowledge.Combine(currentKnowledge);
      ReplicaState.ForgottenKnowledge.Combine(forgottenKnowledge);
    }

    public void SaveChangeWithChangeUnits(ItemChange change, SaveChangeWithChangeUnitsContext context)
    {
      throw new NotSupportedException("INotifyingChangeApplierTarget.SaveChangeWithChangeUnits");
    }

    public void SaveConflict(ItemChange conflictingChange, object conflictingChangeData, SyncKnowledge conflictingChangeKnowledge)
    {
      throw new NotSupportedException("INotifyingChangeApplierTarget.SaveConflict");
    }

    #endregion

    public void Complete()
    {
      metadataManager.SaveReplicaState();
    }

    public SyncSession(SyncSessionContext syncContext, Session session, SyncConfiguration configuration)
    {
      if (syncContext==null)
        throw new ArgumentNullException("syncContext");
      if (session==null)
        throw new ArgumentNullException("session");
      if (configuration==null)
        throw new ArgumentNullException("configuration");

      this.syncContext = syncContext;
      this.session = session;
      this.configuration = configuration;

      keyMap = new KeyMap();
      keyDependencies = new Dictionary<Key, List<KeyDependency>>();

      accessor = session.Services.Demand<DirectEntityAccessor>();
      metadataFetcher = session.Services.Demand<MetadataFetcher>();
      tickGenerator = session.Services.Demand<SyncTickGenerator>();
      tupleFormatters = session.Services.Demand<EntityTupleFormatterRegistry>();

      metadataManager = session.Services.Demand<MetadataManager>();
      metadataManager.Configure(configuration);
      metadataManager.LoadReplicaState();
    }

    static SyncSession()
    {
      CreateKeyMethod = typeof (Key).GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static, null,
        new[] {typeof (Domain), typeof (TypeInfo), typeof (TypeReferenceAccuracy), typeof (Tuples.Tuple)}, null);
    }
  }
}