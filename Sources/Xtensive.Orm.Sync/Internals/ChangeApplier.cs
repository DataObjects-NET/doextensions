using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Synchronization;
using Xtensive.Orm.Services;
using Xtensive.Orm.Sync.DataExchange;
using Xtensive.Tuples;
using FieldInfo = Xtensive.Orm.Model.FieldInfo;

namespace Xtensive.Orm.Sync
{
  internal sealed class ChangeApplier : INotifyingChangeApplierTarget
  {
    private readonly KeyMap keyMap;
    private readonly Dictionary<Key, List<KeyDependency>> keyDependencies;

    private readonly Session session;
    private readonly MetadataFetcher metadataFetcher;
    private readonly DirectEntityAccessor accessor;
    private readonly ReplicaState replicaState;
    private readonly MetadataManager metadataManager;
    private readonly SyncTickGenerator tickGenerator;
    private readonly EntityTupleFormatterRegistry tupleFormatters;

    public void ProcessChangeBatch(
      ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges,
      IChangeDataRetriever changeDataRetriever, SyncCallbacks syncCallbacks, SyncSessionContext syncContext)
    {
      var localChanges = new List<ItemChange>();
      GetLocalChanges(sourceChanges, localChanges);

      new NotifyingChangeApplier(metadataManager.IdFormats)
        .ApplyChanges(
          resolutionPolicy, sourceChanges, changeDataRetriever,
          localChanges, replicaState.CurrentKnowledge, replicaState.ForgottenKnowledge,
          this, syncContext, syncCallbacks);
    }

    private void GetLocalChanges(ChangeBatch sourceChanges, ICollection<ItemChange> output)
    {
      var ids = sourceChanges.Select(i => i.ItemId.ToString());
      var items = session.Query
        .Execute(q => q.All<SyncInfo>().Where(i => i.Id.In(ids)))
        .ToDictionary(i => i.SyncId);

      foreach (var sourceChange in sourceChanges) {
        var changeKind = ChangeKind.UnknownItem;
        var createdVersion = SyncVersion.UnknownVersion;
        var lastChangeVersion = SyncVersion.UnknownVersion;
        SyncInfo info;
        if (items.TryGetValue(sourceChange.ItemId, out info)) {
          createdVersion = info.CreationVersion;
          if (info.IsTombstone) {
            changeKind = ChangeKind.Deleted;
            lastChangeVersion = info.TombstoneVersion;
          }
          else {
            changeKind = ChangeKind.Update;
            lastChangeVersion = info.ChangeVersion;
          }
        }

        var localChange = new ItemChange(
          metadataManager.IdFormats, replicaState.Id, sourceChange.ItemId,
          changeKind, createdVersion, lastChangeVersion);

        localChange.SetAllChangeUnitsPresent();
        output.Add(localChange);
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
          mappedKey = KeyFactory.CreateKey(session.Domain, typeInfo, targetTuple);
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

    #region INotifyingChangeApplierTarget

    SyncIdFormatGroup INotifyingChangeApplierTarget.IdFormats { get { return metadataManager.IdFormats; } }

    void INotifyingChangeApplierTarget.SaveItemChange(SaveChangeAction saveChangeAction, ItemChange change, SaveChangeContext context)
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

    bool INotifyingChangeApplierTarget.TryGetDestinationVersion(ItemChange sourceChange, out ItemChange destinationVersion)
    {
      throw new NotSupportedException("INotifyingChangeApplierTarget.TryGetDestinationVersion");
    }

    ulong INotifyingChangeApplierTarget.GetNextTickCount()
    {
      return (ulong) tickGenerator.GetNextTick();
    }

    IChangeDataRetriever INotifyingChangeApplierTarget.GetDataRetriever()
    {
      throw new NotSupportedException("INotifyingChangeApplierTarget.GetDataRetriever");
    }

    void INotifyingChangeApplierTarget.StoreKnowledgeForScope(SyncKnowledge currentKnowledge, ForgottenKnowledge forgottenKnowledge)
    {
      replicaState.CurrentKnowledge.Combine(currentKnowledge);
      replicaState.ForgottenKnowledge.Combine(forgottenKnowledge);
    }

    void INotifyingChangeApplierTarget.SaveChangeWithChangeUnits(ItemChange change, SaveChangeWithChangeUnitsContext context)
    {
      throw new NotSupportedException("INotifyingChangeApplierTarget.SaveChangeWithChangeUnits");
    }

    void INotifyingChangeApplierTarget.SaveConflict(ItemChange conflictingChange, object conflictingChangeData, SyncKnowledge conflictingChangeKnowledge)
    {
      throw new NotSupportedException("INotifyingChangeApplierTarget.SaveConflict");
    }

    #endregion

    public ChangeApplier(
      ReplicaState replicaState,
      MetadataManager metadataManager, MetadataFetcher metadataFetcher,
      DirectEntityAccessor accessor, SyncTickGenerator tickGenerator,
      EntityTupleFormatterRegistry tupleFormatters)
    {
      if (replicaState==null)
        throw new ArgumentNullException("replicaState");
      if (metadataManager==null)
        throw new ArgumentNullException("metadataManager");
      if (metadataFetcher==null)
        throw new ArgumentNullException("metadataFetcher");
      if (accessor==null)
        throw new ArgumentNullException("accessor");
      if (tickGenerator==null)
        throw new ArgumentNullException("tickGenerator");
      if (tupleFormatters==null)
        throw new ArgumentNullException("tupleFormatters");

      this.replicaState = replicaState;
      this.metadataManager = metadataManager;
      this.metadataFetcher = metadataFetcher;
      this.accessor = accessor;
      this.tickGenerator = tickGenerator;
      this.tupleFormatters = tupleFormatters;

      session = metadataManager.Session;

      keyMap = new KeyMap();
      keyDependencies = new Dictionary<Key, List<KeyDependency>>();
    }
  }
}