using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Synchronization;
using Xtensive.Collections.Graphs;
using Xtensive.IoC;
using Xtensive.Orm.Model;
using Xtensive.Orm.Services;
using Xtensive.Orm.Sync.DataExchange;
using FieldInfo = Xtensive.Orm.Model.FieldInfo;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (MetadataManager), Singleton = false)]
  internal sealed class MetadataManager : ISessionService
  {
    private readonly EntityTupleFormatterRegistry tupleFormatters;
    private readonly SyncTickGenerator tickGenerator;
    private readonly ReplicaManager replicaManager;
    private readonly DirectEntityAccessor entityAccessor;
    private readonly GlobalTypeIdRegistry typeIdRegistry;

    private readonly Session session;

    private ReplicaState replicaState;
    private SyncConfiguration configuration;
    private KeyTracker keyTracker;

    private List<MetadataStore> storeList;
    private Dictionary<Type, MetadataStore> storeIndex;

    public ReplicaState ReplicaState { get { return replicaState; } }

    public void Configure(SyncConfiguration newConfiguration)
    {
      if (newConfiguration==null)
        throw new ArgumentNullException("newConfiguration");
      configuration = newConfiguration;
      keyTracker = new KeyTracker(configuration);
    }

    public void LoadReplicaState()
    {
      replicaState = replicaManager.LoadReplicaState();
    }

    public void SaveReplicaState()
    {
      replicaManager.SaveReplicaState(replicaState);
    }

    public IEnumerable<ChangeSet> DetectChanges(uint batchSize, SyncKnowledge destinationKnowledge)
    {
      var mappedKnowledge = replicaState.CurrentKnowledge.MapRemoteKnowledgeToLocal(destinationKnowledge);
      mappedKnowledge.ReplicaKeyMap.FindOrAddReplicaKey(replicaState.Id);

      var stores = storeList.AsEnumerable();

      if (configuration.SyncTypes.Count > 0)
        stores = stores.Where(s => s.EntityType.In(configuration.SyncTypes));

      if (configuration.SkipTypes.Count > 0)
        stores = stores.Where(s => !s.EntityType.In(configuration.SkipTypes));

      foreach (var store in stores) {
        Expression filter;
        configuration.Filters.TryGetValue(store.EntityType, out filter);
        var items = store.GetMetadata(filter);
        var batches = DetectChanges(items, batchSize, mappedKnowledge);
        foreach (var batch in batches)
          yield return batch;
      }

      while (keyTracker.HasKeysToSync) {
        var keys = keyTracker.GetKeysToSync();
        var groups = keys.GroupBy(i => i.TypeReference.Type.Hierarchy.Root);

        foreach (var group in groups) {
          var store = GetStore(group.Key);
          var items = store.GetMetadata(group.ToList());
          var batches = DetectChanges(items, batchSize, mappedKnowledge);
          foreach (var batch in batches)
            yield return batch;
        }
      }
    }

    private IEnumerable<ChangeSet> DetectChanges(IEnumerable<SyncInfo> items, uint batchSize, SyncKnowledge mappedKnowledge)
    {
      int itemCount = 0;
      var result = new ChangeSet();
      var references = new HashSet<Key>();

      foreach (var item in items) {

        var createdVersion = item.CreationVersion;
        var lastChangeVersion = item.ChangeVersion;
        var changeKind = ChangeKind.Update;

        if (item.IsTombstone) {
          changeKind = ChangeKind.Deleted;
          lastChangeVersion = item.TombstoneVersion;
        }

        if (mappedKnowledge.Contains(replicaState.Id, item.SyncId, lastChangeVersion)) {
          keyTracker.UnrequestKeySync(item.SyncTargetKey);
          continue;
        }

        var change = new ItemChange(WellKnown.IdFormats, replicaState.Id, item.SyncId, changeKind, createdVersion, lastChangeVersion);
        var changeData = new ItemChangeData {
          Change = change,
          Identity = new Identity(item.SyncTargetKey, item.SyncId),
        };

        if (!item.IsTombstone) {
          keyTracker.RegisterKeySync(item.SyncTargetKey);
          var syncTarget = item.SyncTarget;
          var entityTuple = entityAccessor.GetEntityState(syncTarget).Tuple;
          changeData.TupleValue = tupleFormatters.Get(syncTarget.TypeInfo.UnderlyingType).Format(entityTuple);
          var type = item.SyncTargetKey.TypeInfo;
          var fields = type.Fields.Where(f => f.IsEntity);
          foreach (var field in fields) {
            var key = entityAccessor.GetReferenceKey(syncTarget, field);
            if (key!=null) {
              changeData.References.Add(field.Name, new Identity(key));
              references.Add(key);
              entityAccessor.SetReferenceKey(syncTarget, field, null);
            }
          }
        }

        result.Add(changeData);
        itemCount++;

        if (itemCount!=batchSize)
          continue;

        if (references.Count > 0)
          LoadReferences(result, references);

        yield return result;
        itemCount = 0;
        result = new ChangeSet();
        references = new HashSet<Key>();
      }

      if (result.Count > 0) {
        if (references.Count > 0)
          LoadReferences(result, references);
        yield return result;
      }
    }

    private void LoadReferences(IEnumerable<ItemChangeData> items, IEnumerable<Key> keys)
    {
      var lookup = GetMetadata(keys.ToList())
        .Distinct()
        .ToDictionary(i => i.SyncTargetKey);

      foreach (var item in items)
        foreach (var reference in item.References.Values) {
          SyncInfo syncInfo;
          if (lookup.TryGetValue(reference.Key, out syncInfo)) {
            reference.Key = syncInfo.SyncTargetKey;
            reference.GlobalId = syncInfo.SyncId;
            keyTracker.RequestKeySync(syncInfo.SyncTargetKey);
          }
        }
    }

    public void GetLocalChanges(ChangeBatch sourceChanges, ICollection<ItemChange> output)
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
          WellKnown.IdFormats, replicaState.Id, sourceChange.ItemId,
          changeKind, createdVersion, lastChangeVersion);

        localChange.SetAllChangeUnitsPresent();
        output.Add(localChange);
      }
    }

    public IEnumerable<SyncInfo> GetMetadata(IEnumerable<Key> keys)
    {
      var groups = keys.GroupBy(i => i.TypeReference.Type);

      foreach (var group in groups) {
        MetadataStore store;
        var originalType = group.Key;

        if (originalType.IsInterface) {
          var rootTypes = originalType.GetImplementors(false)
            .Select(i => i.Hierarchy.Root);
          foreach (var rootType in rootTypes) {
            store = GetStore(rootType);
            foreach (var item in store.GetMetadata(group.ToList()))
              yield return item;
          }
        }
        else {
          store = GetStore(originalType);
          foreach (var item in store.GetMetadata(group.ToList()))
            yield return item;
        }
      }
    }

    public SyncInfo CreateMetadata(Key key)
    {
      var store = GetStore(key.TypeInfo);
      var globalTypeId = typeIdRegistry.GetGlobalTypeId(key.TypeInfo.UnderlyingType);
      var tick = tickGenerator.GetNextTick();
      var syncId = SyncIdBuilder.GetSyncId(globalTypeId, replicaState.Id, tick);
      var result = store.CreateMetadata(syncId, key);

      result.CreatedReplicaKey = WellKnown.LocalReplicaKey;
      result.CreatedTickCount = tick;
      result.ChangeReplicaKey = WellKnown.LocalReplicaKey;
      result.ChangeTickCount = tick;

      return result;
    }

    public SyncInfo CreateMetadata(Key key, ItemChange change)
    {
      var store = GetStore(key.TypeInfo);
      var result = store.CreateMetadata(change.ItemId, key);
      result.CreationVersion = change.CreationVersion;
      result.ChangeVersion = change.ChangeVersion;
      return result;
    }

    public void UpdateMetadata(SyncInfo item, bool markAsTombstone)
    {
      long tick = tickGenerator.GetNextTick();
      item.ChangeReplicaKey = WellKnown.LocalReplicaKey;
      item.ChangeTickCount = tick;

      if (!markAsTombstone)
        return;

      item.TombstoneReplicaKey = WellKnown.LocalReplicaKey;
      item.TombstoneTickCount = tick;
      item.IsTombstone = true;
    }

    public void UpdateMetadata(SyncInfo item, ItemChange change, bool markAsTombstone)
    {
      item.ChangeVersion = change.ChangeVersion;

      if (!markAsTombstone)
        return;

      item.TombstoneVersion = change.ChangeVersion;
      item.IsTombstone = true;
    }

    private MetadataStore GetStore(TypeInfo type)
    {
      MetadataStore store;
      if (storeIndex.TryGetValue(type.Hierarchy.Root.UnderlyingType, out store))
        return store;
      throw new InvalidOperationException(string.Format("Store for type '{0}' is not registered", type));
    }

    private void InitializeStores()
    {
      var graph = new Graph<Node<Type>, Edge>();
      var nodeIndex = new Dictionary<Type, Node<Type>>();
      var model = session.Domain.Model;

      var types = model.Types[typeof (SyncInfo)].GetDescendants()
        .Select(t => t.UnderlyingType.GetGenericArguments().First());
      foreach (var type in types) {
        var node = new Node<Type>(type);
        nodeIndex[type] = node;
        graph.Nodes.Add(node);
      }

      foreach (var right in graph.Nodes) {
        var keyFields = model.Types[right.Value].Hierarchy.Key.Fields;
        foreach (var field in keyFields) {
          if (!field.IsEntity)
            continue;
          var left = nodeIndex[model.Types[field.ValueType].GetRoot().UnderlyingType];
          new Edge<FieldInfo>(left, right).Attach();
        }
      }

      var result = TopologicalSorter.Sort(graph);
      var rootTypes = result.SortedNodes.Select(n => n.Value).ToList();

      storeList = new List<MetadataStore>(rootTypes.Count);
      storeIndex = new Dictionary<Type, MetadataStore>(rootTypes.Count);

      foreach (var rootType in rootTypes) {
        var storeType = typeof (MetadataStore<>).MakeGenericType(rootType);
        var storeInstance = (MetadataStore) Activator.CreateInstance(storeType, session);
        storeList.Add(storeInstance);
        storeIndex[rootType] = storeInstance;
      }
    }

    [ServiceConstructor]
    public MetadataManager(
      Session session, GlobalTypeIdRegistry typeIdRegistry,
      EntityTupleFormatterRegistry tupleFormatters, SyncTickGenerator tickGenerator,
      ReplicaManager replicaManager, DirectEntityAccessor entityAccessor)
    {
      if (session==null)
        throw new ArgumentNullException("session");
      if (typeIdRegistry==null)
        throw new ArgumentNullException("typeIdRegistry");
      if (tupleFormatters==null)
        throw new ArgumentNullException("tupleFormatters");
      if (tickGenerator==null)
        throw new ArgumentNullException("tickGenerator");
      if (replicaManager==null)
        throw new ArgumentNullException("replicaManager");
      if (entityAccessor==null)
        throw new ArgumentNullException("entityAccessor");

      this.session = session;
      this.typeIdRegistry = typeIdRegistry;
      this.tupleFormatters = tupleFormatters;
      this.tickGenerator = tickGenerator;
      this.replicaManager = replicaManager;
      this.entityAccessor = entityAccessor;

      InitializeStores();
      Configure(new SyncConfiguration());
    }
  }
}