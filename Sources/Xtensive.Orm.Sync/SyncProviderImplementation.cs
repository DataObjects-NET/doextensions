using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Synchronization;
using Xtensive.IoC;
using Xtensive.Orm.Services;
using Xtensive.Tuples;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (SyncProviderImplementation))]
  public class SyncProviderImplementation : KnowledgeSyncProvider,
    IChangeDataRetriever,
    INotifyingChangeApplierTarget,
    ISessionService
  {
    private readonly Session session;
    private readonly SyncMetadataStore metadataStore;
    private readonly KeyMap keyMap;
    private readonly SyncRootSet syncRoots;
    private readonly DirectEntityAccessor accessor;
    private readonly Dictionary<Key, List<EntityStub>> keyDependencies;

    private SyncSessionContext syncContext;
    private IEnumerator<ChangeSet> changeSetEnumerator;
    private ChangeSet currentChangeSet;

    public override SyncIdFormatGroup IdFormats
    {
      get { return Wellknown.IdFormats; }
    }

    public override void BeginSession(SyncProviderPosition position, SyncSessionContext syncSessionContext)
    {
      syncContext = syncSessionContext;
    }

    public override void EndSession(SyncSessionContext syncSessionContext)
    {
    }

    #region Source provider methods

    public IChangeDataRetriever GetDataRetriever()
    {
      return this;
    }

    public override void GetSyncBatchParameters(out uint batchSize, out SyncKnowledge knowledge)
    {
      batchSize = Wellknown.SyncBatchSize;
      knowledge = metadataStore.CurrentKnowledge;
    }

    public override ChangeBatch GetChangeBatch(uint batchSize, SyncKnowledge destinationKnowledge,
      out object changeDataRetriever)
    {
      changeDataRetriever = this;
      var result = new ChangeBatch(IdFormats, destinationKnowledge, metadataStore.ForgottenKnowledge);
      bool hasNext;

      if (changeSetEnumerator==null) {
        var changeSets = metadataStore.DetectChanges(batchSize, destinationKnowledge);
        changeSetEnumerator = changeSets.GetEnumerator();
        hasNext = changeSetEnumerator.MoveNext();
        if (!hasNext) {
          result.BeginUnorderedGroup();
          result.EndUnorderedGroup(metadataStore.CurrentKnowledge, true);
          result.SetLastBatch();
          return result;
        }
      }

      result.BeginUnorderedGroup();
      currentChangeSet = changeSetEnumerator.Current;
      result.AddChanges(currentChangeSet.GetItemChanges());

      hasNext = changeSetEnumerator.MoveNext();
      if (!hasNext) {
        result.EndUnorderedGroup(metadataStore.CurrentKnowledge, true);
        result.SetLastBatch();
      }
      else
        result.EndUnorderedGroup(metadataStore.CurrentKnowledge, false);

      return result;
    }

    public object LoadChangeData(LoadChangeContext loadChangeContext)
    {
      var id = loadChangeContext.ItemChange.ItemId.GetGuidId();
      return currentChangeSet[id];
    }

    #endregion

    #region Destination provider methods

    public override void ProcessChangeBatch(ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges,
      object changeDataRetriever, SyncCallbacks syncCallbacks, SyncSessionStatistics sessionStatistics)
    {
      var localChanges = metadataStore.GetLocalChanges(sourceChanges).ToList();
      var knowledge = metadataStore.CurrentKnowledge.Clone();
      var forgottenKnowledge = metadataStore.ForgottenKnowledge;
      var changeApplier = new NotifyingChangeApplier(IdFormats);

      changeApplier.ApplyChanges(resolutionPolicy, sourceChanges, changeDataRetriever as IChangeDataRetriever,
        localChanges, knowledge, forgottenKnowledge, this, syncContext, syncCallbacks);
    }

    public void SaveItemChange(SaveChangeAction saveChangeAction, ItemChange change, SaveChangeContext context)
    {
      var data = context.ChangeData as ItemChangeData;
      if (data != null)
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
      var hierarchy = session.Domain.Model.Types[entityType].Hierarchy;
      Key mappedKey = null;
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
            if (mappedRefKey == null)
              throw new InvalidOperationException(string.Format("Mapped key for original key '{0}'", identity.Key.Format()));
            mappedRefKey.Value.CopyTo(targetTuple, field.MappingInfo.Offset);
          }
          mappedKey = Key.Create(session.Domain, entityType, targetTuple);
          break;
      }
      RegisterKeyMapping(data, mappedKey);
      metadataStore.CreateMetadata(mappedKey, data.Change);
      var entity = accessor.CreateEntity(data.Identity.Key.TypeInfo.UnderlyingType, mappedKey.Value);
      var state = accessor.GetEntityState(entity);
      var offset = mappedKey.Value.Count;
      data.Tuple.CopyTo(state.Tuple, offset, offset, data.Tuple.Count - offset);

      var stub = new EntityStub(state, session.DisableSaveChanges(entity));
      UpdateReferences(stub, data.References);
      TryRemovePin(stub);
    }

    private void HandleUpdateEntity(ItemChangeData data)
    {
      var syncInfo = session.Query.All<SyncInfo>().Single(s => s.GlobalId==data.Identity.GlobalId);
      metadataStore.UpdateMetadata(syncInfo, data.Change, false);
      var entity = syncInfo.SyncTarget;
      var state = accessor.GetEntityState(entity);
      var offset = entity.Key.Value.Count;
      data.Tuple.CopyTo(state.DifferentialTuple, offset, offset, data.Tuple.Count - offset);
      state.PersistenceState = PersistenceState.Modified;
      var stub = new EntityStub(state, session.DisableSaveChanges(entity));
      UpdateReferences(stub, data.References);
      TryRemovePin(stub);
    }

    private void HandleRemoveEntity(ItemChange change)
    {
      var syncInfo = session.Query.All<SyncInfo>().Single(s => s.GlobalId==change.ItemId.GetGuidId());
      metadataStore.UpdateMetadata(syncInfo, change, true);
      var entity = syncInfo.SyncTarget;
      var state = accessor.GetEntityState(entity);
      state.PersistenceState = PersistenceState.Removed;
    }

    private static void TryRemovePin(EntityStub stub)
    {
      if (stub.References.Count==0)
        stub.Pin.Dispose();
    }

    private void RegisterKeyMapping(ItemChangeData data, Key mappedKey)
    {
      keyMap.Register(data.Identity, mappedKey);
      List<EntityStub> stubs;
      if (!keyDependencies.TryGetValue(data.Identity.Key, out stubs))
        return;

      foreach (var stub in stubs) {
        var references = stub.References.Where(r => r.Value.Key == data.Identity.Key).ToList();
        foreach (var reference in references) {
          accessor.SetReferenceKey(stub.State.Entity, reference.Field, mappedKey);
          stub.References.Remove(reference);
        }
        TryRemovePin(stub);
      }
    }

    private Key TryResolveIdentity(Identity reference)
    {
      return keyMap.Resolve(reference);
    }

    private void UpdateReferences(EntityStub stub, Dictionary<string, Identity> references)
    {
      var typeInfo = stub.State.Type;
      foreach (var field in typeInfo.Fields.Where(f => f.IsEntity && !f.IsPrimaryKey)) {
        Identity reference;
        if (!references.TryGetValue(field.Name, out reference))
          continue;
        var mappedKey = TryResolveIdentity(reference);
        if (mappedKey==null) {
          RegisterReferenceDependency(stub, new Reference(field, reference));
        }
        else
          //mappedKey.Value.CopyTo(state.Tuple, field.MappingInfo.Offset);
          accessor.SetReferenceKey(stub.State.Entity, field, mappedKey);
      }
    }

    private void RegisterReferenceDependency(EntityStub stub, Reference reference)
    {
      stub.References.Add(reference);
      List<EntityStub> container;
      if (!keyDependencies.TryGetValue(reference.Value.Key, out container)) {
        container = new List<EntityStub>();
        keyDependencies[reference.Value.Key] = container;
      }
      container.Add(stub);
    }

    #endregion

    public ulong GetNextTickCount()
    {
      return (ulong) metadataStore.NextTick;
    }

    public void SaveConflict(ItemChange conflictingChange, object conflictingChangeData, SyncKnowledge conflictingChangeKnowledge)
    {
      throw new NotImplementedException();
    }

    public void StoreKnowledgeForScope(SyncKnowledge newCurrentKnowledge, ForgottenKnowledge newForgottenKnowledge)
    {
      metadataStore.UpdateKnowledge(newCurrentKnowledge, newForgottenKnowledge);
    }

    public bool TryGetDestinationVersion(ItemChange sourceChange, out ItemChange destinationVersion)
    {
      throw new NotImplementedException();
    }

    #region Not supported methods

    public override FullEnumerationChangeBatch GetFullEnumerationChangeBatch(uint batchSize, SyncId lowerEnumerationBound, SyncKnowledge knowledgeForDataRetrieval, out object changeDataRetriever)
    {
      throw new NotSupportedException();
    }

    public override void ProcessFullEnumerationChangeBatch(ConflictResolutionPolicy resolutionPolicy, FullEnumerationChangeBatch sourceChanges, object changeDataRetriever, SyncCallbacks syncCallbacks, SyncSessionStatistics sessionStatistics)
    {
      throw new NotSupportedException();
    }

    public void SaveChangeWithChangeUnits(ItemChange change, SaveChangeWithChangeUnitsContext context)
    {
      throw new NotSupportedException();
    }

    #endregion

    [ServiceConstructor]
    public SyncProviderImplementation(Session session)
    {
      this.session = session;
      accessor = session.Services.Get<DirectEntityAccessor>();
      syncRoots = new SyncRootSet(session.Domain.Model);
      metadataStore = new SyncMetadataStore(session, syncRoots);
      keyMap = new KeyMap(session, syncRoots);
      keyDependencies = new Dictionary<Key,List<EntityStub>>();
    }
  }
}