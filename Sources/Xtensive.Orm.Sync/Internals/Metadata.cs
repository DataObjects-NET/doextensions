using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Synchronization;
using Xtensive.Graphs;
using FieldInfo = Xtensive.Orm.Model.FieldInfo;

namespace Xtensive.Orm.Sync
{
  internal class Metadata : SessionBound
  {
    private readonly SyncConfiguration configuration;

    private List<MetadataStore> storeList;
    private Dictionary<Type, MetadataStore> storeIndex;

    public Replica Replica { get; private set; }

    public IEnumerable<ChangeSet> DetectChanges(uint batchSize, SyncKnowledge destinationKnowledge)
    {
      var mappedKnowledge = Replica.CurrentKnowledge.MapRemoteKnowledgeToLocal(destinationKnowledge);
      mappedKnowledge.ReplicaKeyMap.FindOrAddReplicaKey(Replica.Id);

      var stores = storeList.AsEnumerable();
      if (configuration.Types.Count > 0)
        stores = stores.Where(r => configuration.Types.Contains(r.EntityType));

      foreach (var store in stores) {
        var changeSets = DetectChanges(store, batchSize, mappedKnowledge);
        foreach (var changeSet in changeSets)
          yield return changeSet;
      }
    }

    private IEnumerable<ChangeSet> DetectChanges(MetadataStore store, uint batchSize, SyncKnowledge mappedKnowledge)
    {
      int itemCount = 0;
      var result = new ChangeSet();
      var references = new HashSet<Key>();
      var items = store.GetMetadata();

      foreach (var item in items) {

        var createdVersion = item.CreationVersion;
        var lastChangeVersion = item.ChangeVersion;
        var changeKind = ChangeKind.Update;

        if (item.IsTombstone) {
          changeKind = ChangeKind.Deleted;
          lastChangeVersion = item.TombstoneVersion;
        }

        if (mappedKnowledge.Contains(Replica.Id, item.SyncId, lastChangeVersion))
          continue;

        var change = new ItemChange(Wellknown.IdFormats, Replica.Id, item.SyncId, changeKind, createdVersion, lastChangeVersion);
        var changeData = new ItemChangeData {
          Change = change,
          Identity = new Identity(item.GlobalId, item.SyncTargetKey),
        };
        if (!item.IsTombstone) {
          changeData.Tuple = store.EntityAccessor.GetEntityState(item.SyncTarget).Tuple.Clone();
          var type = item.SyncTargetKey.TypeInfo;
          var fields = type.Fields.Where(f => f.IsEntity);
          foreach (var field in fields) {
            var key = store.EntityAccessor.GetReferenceKey(item.SyncTarget, field);
            if (key!=null) {
              changeData.References.Add(field.Name, new Identity(key));
              references.Add(key);
              store.EntityAccessor.SetReferenceKey(item.SyncTarget, field, null);
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
      if (result.Any()) {
        if (references.Count > 0)
          LoadReferences(result, references);
        yield return result;
      }
    }

    private void LoadReferences(IEnumerable<ItemChangeData> result, IEnumerable<Key> keys)
    {
      var lookup = GetMetadata(keys.ToList())
        .Distinct()
        .ToDictionary(i => i.SyncTargetKey);

      foreach (var data in result)
        foreach (var reference in data.References.Values) {
          SyncInfo item;
          if (lookup.TryGetValue(reference.Key, out item)) {
            reference.Key = item.SyncTargetKey;
            reference.GlobalId = item.GlobalId;
          }
        }
    }

    public IEnumerable<ItemChange> GetLocalChanges(IEnumerable<ItemChange> changes)
    {
      // TODO: fix this

      var ids = changes
        .Select(i => i.ItemId.GetGuidId());
      var items = Session.Query.All<SyncInfo>()
        .Where(i => i.GlobalId.In(ids))
        .ToDictionary(i => i.GlobalId);

      foreach (var change in changes) {

        var changeKind = ChangeKind.UnknownItem;
        var createdVersion = SyncVersion.UnknownVersion;
        var lastChangeVersion = SyncVersion.UnknownVersion;
        
        SyncInfo info;
        if (items.TryGetValue(change.ItemId.GetGuidId(), out info)) {
          changeKind = ChangeKind.Update;
          createdVersion = info.CreationVersion;
          lastChangeVersion = info.ChangeVersion;
          if (info.IsTombstone) {
            changeKind = ChangeKind.Deleted;
            lastChangeVersion = info.TombstoneVersion;
          }
        }

        var localChange = new ItemChange(Wellknown.IdFormats, Replica.Id, change.ItemId, changeKind, createdVersion, lastChangeVersion);
        localChange.SetAllChangeUnitsPresent();
        yield return localChange;
      }
    }

    public IEnumerable<SyncInfo> GetMetadata(IEnumerable<Key> keys)
    {
      var groups = keys.GroupBy(i => i.TypeReference.Type.Hierarchy.Root);

      foreach (var @group in groups) {
        MetadataStore store;
        if (!storeIndex.TryGetValue(@group.Key.UnderlyingType, out store))
          continue;

        var items = store.GetMetadata(@group.ToList());
        foreach (var item in items)
          yield return item;
      }
    }

    public SyncInfo GetMetadata(Guid globalId)
    {
      var result = Session.Query.All<SyncInfo>()
        .SingleOrDefault(s => s.GlobalId == globalId);

      if (result == null)
        return null;

      var store = storeList
        .SingleOrDefault(s => s.ItemType == result.GetType());

      if (store == null)
        return result;

      return store.GetMetadata(result);
    }

    public SyncInfo CreateMetadata(Key key)
    {
      MetadataStore store;
      if (!storeIndex.TryGetValue(key.TypeInfo.UnderlyingType, out store))
        return null;

      long tick = Replica.NextTick;
      var result = store.CreateItem(key);
      result.CreatedReplicaKey = Wellknown.LocalReplicaKey;
      result.CreatedTickCount = tick;
      result.ChangeReplicaKey = Wellknown.LocalReplicaKey;
      result.ChangeTickCount = tick;
      result.GlobalId = Guid.NewGuid();
      return result;
    }

    public SyncInfo CreateMetadata(Key key, ItemChange change)
    {
      MetadataStore store;
      if (!storeIndex.TryGetValue(key.TypeInfo.UnderlyingType, out store))
        return null;
      var result = store.CreateItem(key);
      result.GlobalId = change.ItemId.GetGuidId();
      result.CreationVersion = change.CreationVersion;
      result.ChangeVersion = change.ChangeVersion;
      return result;
    }

    public void UpdateMetadata(SyncInfo item, bool markAsTombstone)
    {
      long tick = Replica.NextTick;
      item.ChangeReplicaKey = Wellknown.LocalReplicaKey;
      item.ChangeTickCount = tick;

      if (!markAsTombstone)
        return;

      item.TombstoneReplicaKey = Wellknown.LocalReplicaKey;
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

    private void InitializeStores()
    {
      var graph = new Graph<Node<Type>, Edge>();
      var nodeIndex = new Dictionary<Type, Node<Type>>();
      var model = Session.Domain.Model;

      var types = model.Types[typeof(SyncInfo)].GetDescendants();
      foreach (var type in types) {
        var node = new Node<Type>(type.UnderlyingType);
        nodeIndex[type.UnderlyingType] = node;
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
        var typeInfo = model.Types[rootType];
        var itemType = typeInfo.Fields[Wellknown.EntityFieldName].ValueType; 
        var storeType = typeof(MetadataStore<>).MakeGenericType(itemType);
        var storeInstance = (MetadataStore)Activator.CreateInstance(storeType, Session);
        storeList.Add(storeInstance);
        storeIndex[itemType] = storeInstance;
      }
    }

    public Metadata(Session session, SyncConfiguration configuration)
      : base(session)
    {
      this.configuration = configuration;
      Replica = new Replica(session);
      InitializeStores();
    }
  }
}