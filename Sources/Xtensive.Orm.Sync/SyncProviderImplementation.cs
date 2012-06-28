using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Synchronization;
using Xtensive.Core;
using Xtensive.IoC;
using Xtensive.Orm.Model;
using Xtensive.Orm.Services;
using Xtensive.Tuples;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// <see cref="KnowledgeSyncProvider"/> implementation.
  /// </summary>
  public class SyncProviderImplementation : KnowledgeSyncProvider,
    IChangeDataRetriever,
    INotifyingChangeApplierTarget,
    ISessionService
  {
    private readonly Session session;
    private readonly Metadata metadata;
    private readonly KeyMap keyMap;
    private readonly DirectEntityAccessor accessor;
    private readonly Dictionary<Key, List<KeyDependency>> keyDependencies;

    private SyncSessionContext syncContext;
    private IEnumerator<ChangeSet> changeSetEnumerator;
    private ChangeSet currentChangeSet;

    /// <summary>
    /// When overridden in a derived class, gets the ID format schema of the provider.
    /// </summary>
    /// <returns>The ID format schema of the provider.</returns>
    public override SyncIdFormatGroup IdFormats
    {
      get { return Wellknown.IdFormats; }
    }

    /// <summary>
    /// Gets the configuration settings for the provider.
    /// </summary>
    /// <returns>The configuration settings for the provider.</returns>
    public new SyncConfiguration Configuration { get; private set; }

    /// <summary>
    /// When overridden in a derived class, notifies the provider that it is joining a synchronization session.
    /// </summary>
    /// <param name="position">The position of this provider, relative to the other provider in the session.</param>
    /// <param name="syncSessionContext">The current status of the corresponding session.</param>
    public override void BeginSession(SyncProviderPosition position, SyncSessionContext syncSessionContext)
    {
      syncContext = syncSessionContext;
    }

    /// <summary>
    /// When overridden in a derived class, notifies the provider that a synchronization session to which it was enlisted has completed.
    /// </summary>
    /// <param name="syncSessionContext">The current status of the corresponding session.</param>
    public override void EndSession(SyncSessionContext syncSessionContext)
    {
    }

    #region Source provider methods

    /// <summary>
    /// Gets an object that can be used to retrieve item data from a replica.
    /// </summary>
    /// <returns>
    /// An object that can be used to retrieve item data from a replica.
    /// </returns>
    public IChangeDataRetriever GetDataRetriever()
    {
      return this;
    }

    /// <summary>
    /// When overridden in a derived class, gets the number of item changes that will be included in change batches, and the current knowledge for the synchronization scope.
    /// </summary>
    /// <param name="batchSize">The number of item changes that will be included in change batches returned by this object.</param>
    /// <param name="knowledge">The current knowledge for the synchronization scope, or a newly created knowledge object if no current knowledge exists.</param>
    public override void GetSyncBatchParameters(out uint batchSize, out SyncKnowledge knowledge)
    {
      batchSize = Wellknown.SyncBatchSize;
      knowledge = metadata.Replica.CurrentKnowledge;
    }

    /// <summary>
    /// When overridden in a derived class, gets a change batch that contains item metadata for items that are not contained in the specified knowledge from the destination provider.
    /// </summary>
    /// <param name="batchSize">The number of changes to include in the change batch.</param>
    /// <param name="destinationKnowledge">The knowledge from the destination provider. This knowledge must be mapped by calling <see cref="M:Microsoft.Synchronization.SyncKnowledge.MapRemoteKnowledgeToLocal(Microsoft.Synchronization.SyncKnowledge)"/> on the source knowledge before it can be used for change enumeration.</param>
    /// <param name="changeDataRetriever">Returns an object that can be used to retrieve change data. It can be an <see cref="T:Microsoft.Synchronization.IChangeDataRetriever"/> object or a provider-specific object.</param>
    /// <returns>
    /// A change batch that contains item metadata for items that are not contained in the specified knowledge from the destination provider. Cannot be a null.
    /// </returns>
    public override ChangeBatch GetChangeBatch(uint batchSize, SyncKnowledge destinationKnowledge,
      out object changeDataRetriever)
    {
      changeDataRetriever = this;
      var result = new ChangeBatch(IdFormats, destinationKnowledge, metadata.Replica.ForgottenKnowledge);
      bool hasNext;

      if (changeSetEnumerator==null) {
        var changeSets = metadata.DetectChanges(batchSize, destinationKnowledge);
        changeSetEnumerator = changeSets.GetEnumerator();
        hasNext = changeSetEnumerator.MoveNext();
        if (!hasNext) {
          result.BeginUnorderedGroup();
          result.EndUnorderedGroup(metadata.Replica.CurrentKnowledge, true);
          result.SetLastBatch();
          return result;
        }
      }

      result.BeginUnorderedGroup();
      currentChangeSet = changeSetEnumerator.Current;
      result.AddChanges(currentChangeSet.GetItemChanges());

      hasNext = changeSetEnumerator.MoveNext();
      if (!hasNext) {
        result.EndUnorderedGroup(metadata.Replica.CurrentKnowledge, true);
        result.SetLastBatch();
      }
      else
        result.EndUnorderedGroup(metadata.Replica.CurrentKnowledge, false);

      return result;
    }

    /// <summary>
    /// When overridden in a derived class, this method retrieves item data for a change.
    /// </summary>
    /// <param name="loadChangeContext">Metadata that describes the change for which data should be retrieved.</param>
    /// <returns>
    /// The item data for the change.
    /// </returns>
    public object LoadChangeData(LoadChangeContext loadChangeContext)
    {
      var id = loadChangeContext.ItemChange.ItemId.GetGuidId();
      return currentChangeSet[id];
    }

    #endregion

    #region Destination provider methods

    /// <summary>
    /// When overridden in a derived class, processes a set of changes by detecting conflicts and applying changes to the item store.
    /// </summary>
    /// <param name="resolutionPolicy">The conflict resolution policy to use when this method applies changes.</param>
    /// <param name="sourceChanges">A batch of changes from the source provider to be applied locally.</param>
    /// <param name="changeDataRetriever">An object that can be used to retrieve change data. It can be an <see cref="T:Microsoft.Synchronization.IChangeDataRetriever"/> object or a provider-specific object.</param>
    /// <param name="syncCallbacks">An object that receives event notifications during change application.</param>
    /// <param name="sessionStatistics">Tracks change statistics. For a provider that uses custom change application, this object must be updated with the results of the change application.</param>
    public override void ProcessChangeBatch(ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges,
      object changeDataRetriever, SyncCallbacks syncCallbacks, SyncSessionStatistics sessionStatistics)
    {
      var localChanges = metadata.GetLocalChanges(sourceChanges).ToList();
      var knowledge = metadata.Replica.CurrentKnowledge.Clone();
      var forgottenKnowledge = metadata.Replica.ForgottenKnowledge;
      var changeApplier = new NotifyingChangeApplier(IdFormats);

      changeApplier.ApplyChanges(resolutionPolicy, sourceChanges, changeDataRetriever as IChangeDataRetriever,
        localChanges, knowledge, forgottenKnowledge, this, syncContext, syncCallbacks);
    }

    /// <summary>
    /// When overridden in a derived class, saves an item change to the item store.
    /// </summary>
    /// <param name="saveChangeAction">The action to be performed for the change.</param>
    /// <param name="change">The item change to save.</param>
    /// <param name="context">Information about the change to be applied.</param>
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
            if (mappedRefKey == null)
              throw new InvalidOperationException(string.Format("Mapped key for original key '{0}'", identity.Key.Format()));
            offset = field.MappingInfo.Offset;
            mappedRefKey.Value.CopyTo(targetTuple, 0, offset, field.MappingInfo.Length);
          }
          mappedKey = Key.Create(session.Domain, entityType, targetTuple);
          break;
      }
      RegisterKeyMapping(data, mappedKey);
      metadata.CreateMetadata(mappedKey, data.Change);
      var entity = accessor.CreateEntity(data.Identity.Key.TypeInfo.UnderlyingType, mappedKey.Value);
      var state = accessor.GetEntityState(entity);
      offset = mappedKey.Value.Count;
      data.Tuple.CopyTo(state.Tuple, offset, offset, data.Tuple.Count - offset);
      UpdateReferences(state, data.References);
    }

    private void HandleUpdateEntity(ItemChangeData data)
    {
      var syncInfo = session.Query.All<SyncInfo>().Single(s => s.GlobalId==data.Identity.GlobalId);
      metadata.UpdateMetadata(syncInfo, data.Change, false);
      var entity = syncInfo.SyncTarget;
      var state = accessor.GetEntityState(entity);
      var offset = entity.Key.Value.Count;
      data.Tuple.CopyTo(state.DifferentialTuple, offset, offset, data.Tuple.Count - offset);
      state.PersistenceState = PersistenceState.Modified;
      UpdateReferences(state, data.References);
    }

    private void HandleRemoveEntity(ItemChange change)
    {
      var syncInfo = session.Query.All<SyncInfo>().Single(s => s.GlobalId==change.ItemId.GetGuidId());
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

    /// <summary>
    /// When overridden in a derived class, increments the tick count and returns the new tick count.
    /// </summary>
    /// <returns>
    /// The newly incremented tick count.
    /// </returns>
    public ulong GetNextTickCount()
    {
      return (ulong) metadata.Replica.NextTick;
    }

    /// <summary>
    /// When overridden in a derived class, saves information about a change that caused a conflict.
    /// </summary>
    /// <param name="conflictingChange">The item metadata for the conflicting change.</param>
    /// <param name="conflictingChangeData">The item data for the conflicting change.</param>
    /// <param name="conflictingChangeKnowledge">The knowledge to be learned if this change is applied. This must be saved with the change.</param>
    public void SaveConflict(ItemChange conflictingChange, object conflictingChangeData, SyncKnowledge conflictingChangeKnowledge)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Stores the knowledge for scope.
    /// </summary>
    /// <param name="currentKnowledge">The new current knowledge.</param>
    /// <param name="forgottenKnowledge">The new forgotten knowledge.</param>
    public void StoreKnowledgeForScope(SyncKnowledge currentKnowledge, ForgottenKnowledge forgottenKnowledge)
    {
      metadata.Replica.CurrentKnowledge = currentKnowledge;
      metadata.Replica.ForgottenKnowledge = forgottenKnowledge;
    }

    /// <summary>
    /// Gets the version of an item stored in the destination replica.
    /// </summary>
    /// <param name="sourceChange">The item change that is sent by the source provider.</param>
    /// <param name="destinationVersion">Returns an item change that contains the version of the item in the destination replica.</param>
    /// <returns>
    /// true if the item was found in the destination replica; otherwise, false.
    /// </returns>
    public bool TryGetDestinationVersion(ItemChange sourceChange, out ItemChange destinationVersion)
    {
      throw new NotImplementedException();
    }

    #region Not supported methods

    /// <summary>
    /// When overridden in a derived class, gets a change batch that contains item metadata for items that have IDs greater than the specified lower bound, as part of a full enumeration.
    /// </summary>
    /// <param name="batchSize">The number of changes to include in the change batch.</param>
    /// <param name="lowerEnumerationBound">The lower bound for item IDs. This method returns changes that have IDs greater than or equal to this ID value.</param>
    /// <param name="knowledgeForDataRetrieval">If an item change is contained in this knowledge object, data for that item already exists on the destination replica.</param>
    /// <param name="changeDataRetriever">Returns an object that can be used to retrieve change data. It can be an <see cref="T:Microsoft.Synchronization.IChangeDataRetriever"/> object or a be provider-specific object.</param>
    /// <returns>
    /// A change batch that contains item metadata for items that have IDs greater than the specified lower bound, as part of a full enumeration.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Thrown always</exception>
    public override FullEnumerationChangeBatch GetFullEnumerationChangeBatch(uint batchSize, SyncId lowerEnumerationBound, SyncKnowledge knowledgeForDataRetrieval, out object changeDataRetriever)
    {
      throw new NotSupportedException();
    }

    /// <summary>
    /// When overridden in a derived class, processes a set of changes for a full enumeration by applying changes to the item store.
    /// </summary>
    /// <param name="resolutionPolicy">The conflict resolution policy to use when this method applies changes.</param>
    /// <param name="sourceChanges">A batch of changes from the source provider to be applied locally.</param>
    /// <param name="changeDataRetriever">An object that can be used to retrieve change data. It can be an <see cref="T:Microsoft.Synchronization.IChangeDataRetriever"/> object or a provider-specific object.</param>
    /// <param name="syncCallbacks">An object that receives event notifications during change application.</param>
    /// <param name="sessionStatistics">Tracks change statistics. For a provider that uses custom change application, this object must be updated with the results of the change application.</param>
    /// <exception cref="System.NotSupportedException">Thrown always</exception>
    public override void ProcessFullEnumerationChangeBatch(ConflictResolutionPolicy resolutionPolicy, FullEnumerationChangeBatch sourceChanges, object changeDataRetriever, SyncCallbacks syncCallbacks, SyncSessionStatistics sessionStatistics)
    {
      throw new NotSupportedException();
    }

    /// <summary>
    /// When overridden in a derived class, saves an item change that contains unit change changes to the item store.
    /// </summary>
    /// <param name="change">The item change to apply.</param>
    /// <param name="context">Information about the change to be applied.</param>
    /// <throws>NotSupportedException</throws>
    /// <exception cref="System.NotSupportedException">Thrown always</exception>
    public void SaveChangeWithChangeUnits(ItemChange change, SaveChangeWithChangeUnitsContext context)
    {
      throw new NotSupportedException();
    }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncProviderImplementation"/> class.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="configuration"> </param>
    public SyncProviderImplementation(Session session, SyncConfiguration configuration)
    {
      this.session = session;
      accessor = session.Services.Get<DirectEntityAccessor>();

      Configuration = new SyncConfiguration();
      if (configuration.Types.Count > 0) {
        var roots = configuration.Types
          .Where(t => typeof(IEntity).IsAssignableFrom(t))
          .Select(t => session.Domain.Model.Types[t].GetRoot().UnderlyingType)
          .ToHashSet();
        Configuration.Types.UnionWith(roots);
      }

      metadata = new Metadata(session, configuration);
      keyMap = new KeyMap();
      keyDependencies = new Dictionary<Key,List<KeyDependency>>();
    }
  }
}