using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Synchronization;
using Xtensive.Orm.Model;
using Xtensive.Orm.Services;
using Xtensive.Tuples;
using FieldInfo = Xtensive.Orm.Model.FieldInfo;

namespace Xtensive.Orm.Sync
{
  internal sealed class SyncProviderImplementation : SessionBound
  {
    private static readonly MethodInfo CreateKeyMethod;

    private readonly Metadata metadata;
    private readonly KeyMap keyMap;
    private readonly DirectEntityAccessor accessor;
    private readonly Dictionary<Key, List<KeyDependency>> keyDependencies;
    private readonly EntityTupleFormatterRegistry tupleFormatters;
    private readonly ReplicaManager replicaManager;
    private readonly SyncInfoFetcher syncInfoFetcher;
    private readonly SyncTickGenerator tickGenerator;

    private IEnumerator<ChangeSet> changeSetEnumerator;

    public SyncIdFormatGroup IdFormats { get { return WellKnown.IdFormats; } }
    
    public SyncConfiguration Configuration { get; private set; }

    public Replica Replica { get; private set; }

    public ChangeSet CurrentChangeSet { get; private set; }

    #region Source provider methods

    private bool FilteredBatchIsRequired()
    {
      var c = Configuration;
      return c.SyncTypes.Count > 0 || c.Filters.Count > 0 || c.SkipTypes.Count > 0;
    }

    public ChangeBatch GetChangeBatch(uint batchSize, SyncKnowledge destinationKnowledge)
    {
      ChangeBatch result;
      if (FilteredBatchIsRequired()) {
        var filterInfo = new ItemListFilterInfo(IdFormats);
        result = new ChangeBatch(IdFormats, destinationKnowledge, Replica.ForgottenKnowledge, filterInfo);
      }
      else
        result = new ChangeBatch(IdFormats, destinationKnowledge, Replica.ForgottenKnowledge);

      bool hasNext;

      if (changeSetEnumerator==null) {
        var changeSets = metadata.DetectChanges(batchSize, destinationKnowledge);
        changeSetEnumerator = changeSets.GetEnumerator();
        hasNext = changeSetEnumerator.MoveNext();
        if (!hasNext) {
          result.BeginUnorderedGroup();
          result.EndUnorderedGroup(Replica.CurrentKnowledge, true);
          result.SetLastBatch();
          return result;
        }
      }

      result.BeginUnorderedGroup();
      CurrentChangeSet = changeSetEnumerator.Current;
      result.AddChanges(CurrentChangeSet.GetItemChanges());

      hasNext = changeSetEnumerator.MoveNext();
      if (!hasNext) {
        result.EndUnorderedGroup(Replica.CurrentKnowledge, true);
        result.SetLastBatch();
      }
      else
        result.EndUnorderedGroup(Replica.CurrentKnowledge, false);

      return result;
    }

    #endregion

    #region Destination provider methods

    public void ProcessChangeBatch(ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges,
      object changeDataRetriever, SyncCallbacks syncCallbacks, SyncSessionStatistics sessionStatistics,
      SyncSessionContext syncContext, INotifyingChangeApplierTarget target)
    {
      var localChanges = metadata.GetLocalChanges(sourceChanges).ToList();
      var knowledge = Replica.CurrentKnowledge.Clone();
      var forgottenKnowledge = Replica.ForgottenKnowledge;
      var changeApplier = new NotifyingChangeApplier(IdFormats);

      changeApplier.ApplyChanges(resolutionPolicy, sourceChanges, changeDataRetriever as IChangeDataRetriever,
        localChanges, knowledge, forgottenKnowledge, target, syncContext, syncCallbacks);
    }

    public void SaveItemChange(SaveChangeAction saveChangeAction, ItemChange change, SaveChangeContext context)
    {
      var data = context.ChangeData as ItemChangeData;
      if (data!=null)
        data.Change = change;

      switch (saveChangeAction) {
        case SaveChangeAction.ChangeIdUpdateVersionAndDeleteAndStoreTombstone:
          throw new NotImplementedException();
        case SaveChangeAction.ChangeIdUpdateVersionAndMergeData:
          throw new NotImplementedException();
        case SaveChangeAction.ChangeIdUpdateVersionAndSaveData:
          throw new NotImplementedException();
        case SaveChangeAction.ChangeIdUpdateVersionOnly:
          throw new NotImplementedException();
        case SaveChangeAction.Create:
          HandleCreateEntity(data);
          break;
        case SaveChangeAction.CreateGhost:
          throw new NotImplementedException();
        case SaveChangeAction.DeleteAndRemoveTombstone:
          throw new NotImplementedException();
        case SaveChangeAction.DeleteAndStoreTombstone:
          HandleRemoveEntity(change);
          break;
        case SaveChangeAction.DeleteConflictingAndSaveSourceItem:
          throw new NotImplementedException();
        case SaveChangeAction.DeleteGhostAndStoreTombstone:
          throw new NotImplementedException();
        case SaveChangeAction.DeleteGhostWithoutTombstone:
          throw new NotImplementedException();
        case SaveChangeAction.MarkItemAsGhost:
          throw new NotImplementedException();
        case SaveChangeAction.RenameDestinationAndUpdateVersionData:
          throw new NotImplementedException();
        case SaveChangeAction.RenameSourceAndUpdateVersionAndData:
          throw new NotImplementedException();
        case SaveChangeAction.StoreMergeTombstone:
          throw new NotImplementedException();
        case SaveChangeAction.UnmarkItemAsGhost:
          throw new NotImplementedException();
        case SaveChangeAction.UpdateGhost:
          throw new NotImplementedException();
        case SaveChangeAction.UpdateVersionAndData:
          HandleUpdateEntity(data);
          break;
        case SaveChangeAction.UpdateVersionAndMergeData:
        case SaveChangeAction.UpdateVersionOnly:
          throw new NotImplementedException();
      }
    }

    private void HandleCreateEntity(ItemChangeData data)
    {
      var entityType = data.Identity.Key.TypeReference.Type.UnderlyingType;
      var typeInfo = Session.Domain.Model.Types[entityType];
      var hierarchy = typeInfo.Hierarchy;
      Key mappedKey = null;
      int offset;

      switch (hierarchy.Key.GeneratorKind) {
        case KeyGeneratorKind.Custom:
        case KeyGeneratorKind.Default:
          mappedKey = Key.Create(Session, entityType);
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
          mappedKey = (Key) CreateKeyMethod.Invoke(null, new object[] {Session.Domain, typeInfo, TypeReferenceAccuracy.ExactType, targetTuple});
          break;
      }

      RegisterKeyMapping(data, mappedKey);
      metadata.CreateMetadata(mappedKey, data.Change);
      var entity = accessor.CreateEntity(entityType, mappedKey.Value);
      var state = accessor.GetEntityState(entity);
      offset = mappedKey.Value.Count;
      var changeDataTuple = tupleFormatters.Get(entityType).Parse(data.TupleValue);
      changeDataTuple.CopyTo(state.Tuple, offset, offset, changeDataTuple.Count - offset);
      UpdateReferences(state, data.References);
    }

    private void HandleUpdateEntity(ItemChangeData data)
    {
      var syncInfo = syncInfoFetcher.Fetch(data.Identity.GlobalId);
      if (syncInfo==null)
        return;

      metadata.UpdateMetadata(syncInfo, data.Change, false);
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
      var syncInfo = syncInfoFetcher.Fetch(change.ItemId.GetGuidId());
      if (syncInfo==null)
        return;

      metadata.UpdateMetadata(syncInfo, change, true);
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
      
      var syncInfo = metadata.GetMetadata(identity.GlobalId);
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

    #endregion

    public ulong GetNextTickCount()
    {
      return (ulong) tickGenerator.GetNextTick(Session);
    }

    public void UpdateReplicaState()
    {
      replicaManager.SaveReplica(Replica);
    }

    public void StoreKnowledgeForScope(SyncKnowledge currentKnowledge, ForgottenKnowledge forgottenKnowledge)
    {
      Replica.CurrentKnowledge.Combine(currentKnowledge);
      Replica.ForgottenKnowledge.Combine(forgottenKnowledge);
    }

    public SyncProviderImplementation(Session session, SyncConfiguration configuration)
      : base(session)
    {
      Configuration = configuration;

      accessor = session.Services.Get<DirectEntityAccessor>();
      replicaManager = session.Services.Get<ReplicaManager>();
      syncInfoFetcher = session.Services.Get<SyncInfoFetcher>();

      tickGenerator = session.Domain.Services.Get<SyncTickGenerator>();
      tupleFormatters = session.Domain.Services.Get<EntityTupleFormatterRegistry>();

      Replica = replicaManager.LoadReplica();

      metadata = new Metadata(session, configuration, Replica);
      
      keyMap = new KeyMap();
      keyDependencies = new Dictionary<Key, List<KeyDependency>>();
    }

    static SyncProviderImplementation()
    {
      CreateKeyMethod = typeof (Key).GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static, null,
        new[] {typeof (Domain), typeof (TypeInfo), typeof (TypeReferenceAccuracy), typeof (Tuples.Tuple)}, null);
    }
  }
}