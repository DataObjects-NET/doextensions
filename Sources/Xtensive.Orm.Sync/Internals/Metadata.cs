using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Synchronization;
using Xtensive.Collections.Graphs;
using Xtensive.Core;
using Xtensive.Orm.Model;
using Xtensive.Orm.Sync.DataExchange;
using FieldInfo = Xtensive.Orm.Model.FieldInfo;

namespace Xtensive.Orm.Sync
{
  internal sealed class Metadata : SessionBound
  {
    private readonly SyncConfiguration configuration;
    private readonly HashSet<Key> sentKeys;
    private readonly HashSet<Key> requestedKeys;
    private readonly EntityTupleFormatterRegistry tupleFormatters;
    private readonly SyncTickGenerator tickGenerator;
    private readonly SyncInfoFetcher syncInfoFetcher;
    private readonly GlobalTypeIdRegistry typeIdRegistry;
    private readonly Replica replica;

    private List<MetadataStore> storeList;
    private Dictionary<Type, MetadataStore> storeIndex;

    public IEnumerable<ChangeSet> DetectChanges(uint batchSize, SyncKnowledge destinationKnowledge)
    {
      var mappedKnowledge = replica.CurrentKnowledge.MapRemoteKnowledgeToLocal(destinationKnowledge);
      mappedKnowledge.ReplicaKeyMap.FindOrAddReplicaKey(replica.Id);

      var stores = storeList.AsEnumerable();

      if (configuration.SyncTypes.Count > 0)
        stores = stores.Where(s => s.EntityType.In(configuration.SyncTypes));

      if (configuration.SkipTypes.Count > 0)
        stores = stores.Where(s => !s.EntityType.In(configuration.SkipTypes));

      foreach (var store in stores) {
        Expression filter;
        configuration.Filters.TryGetValue(store.EntityType, out filter);
        var items = store.GetMetadata(filter);
        var batches = DetectChanges(store, items, batchSize, mappedKnowledge);
        foreach (var batch in batches)
          yield return batch;
      }

      while (requestedKeys.Count > 0) {
        var keys = requestedKeys.ToList();
        var groups = keys.GroupBy(i => i.TypeReference.Type.Hierarchy.Root);

        foreach (var @group in groups) {
          var store = GetStore(@group.Key);
          if (store == null)
            continue;

          var items = store.GetMetadata(@group.ToList());
          var batches = DetectChanges(store, items, batchSize, mappedKnowledge);
          foreach (var batch in batches)
            yield return batch;
        }
      }
    }

    private IEnumerable<ChangeSet> DetectChanges(MetadataStore store, IEnumerable<SyncInfo> items, uint batchSize, SyncKnowledge mappedKnowledge)
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

        if (mappedKnowledge.Contains(replica.Id, item.SyncId, lastChangeVersion)) {
          requestedKeys.Remove(item.SyncTargetKey);
          continue;
        }

        var change = new ItemChange(WellKnown.IdFormats, replica.Id, item.SyncId, changeKind, createdVersion, lastChangeVersion);
        var changeData = new ItemChangeData {
          Change = change,
          Identity = new Identity(item.SyncTargetKey, item.SyncId),
        };

        if (!item.IsTombstone) {
          RegisterKeySync(item.SyncTargetKey);
          var syncTarget = item.SyncTarget;
          var entityTuple = store.EntityAccessor.GetEntityState(syncTarget).Tuple;
          changeData.TupleValue = tupleFormatters.Get(syncTarget.TypeInfo.UnderlyingType).Format(entityTuple);
          var type = item.SyncTargetKey.TypeInfo;
          var fields = type.Fields.Where(f => f.IsEntity);
          foreach (var field in fields) {
            var key = store.EntityAccessor.GetReferenceKey(syncTarget, field);
            if (key!=null) {
              changeData.References.Add(field.Name, new Identity(key));
              references.Add(key);
              store.EntityAccessor.SetReferenceKey(syncTarget, field, null);
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
            RequestKeySync(syncInfo.SyncTargetKey);
          }
        }
    }

    private void RegisterKeySync(Key key)
    {
      if (!TypeIsFilteredOrSkipped(key.TypeReference.Type.GetRoot().UnderlyingType))
        return;
      sentKeys.Add(key);
      requestedKeys.Remove(key);
    }

    private void RequestKeySync(Key key)
    {
      if (!TypeIsFilteredOrSkipped(key.TypeReference.Type.GetRoot().UnderlyingType))
        return;
      if (sentKeys.Contains(key))
        return;
      requestedKeys.Add(key);
    }

    private bool TypeIsFilteredOrSkipped(Type type)
    {
      if (configuration.Filters.ContainsKey(type))
        return true;
      if (configuration.SkipTypes.Contains(type))
        return true;
      if (configuration.SyncAll)
        return false;
      return !configuration.SyncTypes.Contains(type);
    }

    public void GetLocalChanges(ChangeBatch sourceChanges, ICollection<ItemChange> output)
    {
      var ids = sourceChanges.Select(i => i.ItemId.ToString());
      var items = Session.Query
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
          WellKnown.IdFormats, replica.Id, sourceChange.ItemId,
          changeKind, createdVersion, lastChangeVersion);

        localChange.SetAllChangeUnitsPresent();
        output.Add(localChange);
      }
    }

    public IEnumerable<SyncInfo> GetMetadata(IEnumerable<Key> keys)
    {
      var groups = keys.GroupBy(i => i.TypeReference.Type);

      foreach (var @group in groups) {
        MetadataStore store;
        var originalType = @group.Key;

        if (originalType.IsInterface) {
          var rootTypes = originalType.GetImplementors(false)
            .Select(i => i.Hierarchy.Root);
          foreach (var rootType in rootTypes) {
            store = GetStore(rootType);
            if (store == null)
              continue;
            foreach (var item in store.GetMetadata(@group.ToList()))
              yield return item;
          }
        }
        else {
          store = GetStore(originalType);
          if (store!=null)
            foreach (var item in store.GetMetadata(@group.ToList()))
              yield return item;
        }
      }
    }

    public SyncInfo GetMetadata(SyncId globalId)
    {
      var syncInfo = syncInfoFetcher.Fetch(globalId);

      if (syncInfo==null)
        return null;

      var store = storeList
        .SingleOrDefault(s => s.InfoType==syncInfo.GetType());

      if (store==null)
        return syncInfo;

      return store.GetMetadata(syncInfo);
    }

    public SyncInfo CreateMetadata(Key key)
    {
      var store = GetStore(key.TypeInfo);
      if (store==null)
        return null;

      var globalTypeId = typeIdRegistry.GetGlobalTypeId(key.TypeInfo.UnderlyingType);
      var tick = tickGenerator.GetNextTick();
      var syncId = SyncIdBuilder.GetSyncId(globalTypeId, replica.Id, tick);
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
      if (store==null)
        return null;
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
      storeIndex.TryGetValue(type.Hierarchy.Root.UnderlyingType, out store);
      return store;
    }

    private void InitializeStores()
    {
      var graph = new Graph<Node<Type>, Edge>();
      var nodeIndex = new Dictionary<Type, Node<Type>>();
      var model = Session.Domain.Model;

      var types = model.Types[typeof(SyncInfo)].GetDescendants()
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
      storeIndex = new Dictionary<Type,MetadataStore>(rootTypes.Count);

      foreach (var rootType in rootTypes) {
        var storeType = typeof(MetadataStore<>).MakeGenericType(rootType);
        var storeInstance = (MetadataStore)Activator.CreateInstance(storeType, Session);
        storeList.Add(storeInstance);
        storeIndex[rootType] = storeInstance;
      }
    }

    public Metadata(Session session, SyncConfiguration configuration, Replica replica)
      : base(session)
    {
      if (session==null)
        throw new ArgumentNullException("session");
      if (configuration==null)
        throw new ArgumentNullException("configuration");
      if (replica==null)
        throw new ArgumentNullException("replica");

      this.configuration = configuration;
      this.replica = replica;

      typeIdRegistry = session.Services.Demand<GlobalTypeIdRegistry>();
      tupleFormatters = session.Services.Demand<EntityTupleFormatterRegistry>();
      tickGenerator = session.Services.Demand<SyncTickGenerator>();
      syncInfoFetcher = session.Services.Demand<SyncInfoFetcher>();

      sentKeys = new HashSet<Key>();
      requestedKeys = new HashSet<Key>();

      InitializeStores();
    }
  }
}