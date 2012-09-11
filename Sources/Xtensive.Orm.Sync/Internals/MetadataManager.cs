using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Synchronization;
using Xtensive.Collections.Graphs;
using Xtensive.IoC;
using Xtensive.Orm.Model;
using FieldInfo = Xtensive.Orm.Model.FieldInfo;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (MetadataManager), Singleton = false)]
  internal sealed class MetadataManager : ISessionService
  {
    private readonly SyncTickGenerator tickGenerator;
    private readonly GlobalTypeIdRegistry typeIdRegistry;
    private readonly Session session;
    private List<MetadataStore> storeList;
    private Dictionary<Type, MetadataStore> storeIndex;

    public Session Session { get { return session; } }

    public SyncIdFormatGroup IdFormats { get { return WellKnown.IdFormats; } }

    public MetadataStore GetStore(TypeInfo type)
    {
      MetadataStore store;
      if (storeIndex.TryGetValue(type.Hierarchy.Root.UnderlyingType, out store))
        return store;
      throw new InvalidOperationException(string.Format("Store for type '{0}' is not registered", type));
    }

    public IEnumerable<MetadataStore> GetStores(SyncConfiguration configuration)
    {
      var stores = storeList.AsEnumerable();

      if (configuration.SyncTypes.Count > 0)
        stores = stores.Where(s => s.EntityType.In(configuration.SyncTypes));

      if (configuration.SkipTypes.Count > 0)
        stores = stores.Where(s => !s.EntityType.In(configuration.SkipTypes));

      return stores;
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
            foreach (var item in store.GetUnorderedMetadata(group.ToList()))
              yield return item;
          }
        }
        else {
          store = GetStore(originalType);
          foreach (var item in store.GetUnorderedMetadata(group.ToList()))
            yield return item;
        }
      }
    }

    public SyncInfo CreateMetadata(Key key, ReplicaState replicaState)
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
    public MetadataManager(Session session, GlobalTypeIdRegistry typeIdRegistry, SyncTickGenerator tickGenerator)
    {
      if (session==null)
        throw new ArgumentNullException("session");
      if (typeIdRegistry==null)
        throw new ArgumentNullException("typeIdRegistry");
      if (tickGenerator==null)
        throw new ArgumentNullException("tickGenerator");

      this.session = session;
      this.typeIdRegistry = typeIdRegistry;
      this.tickGenerator = tickGenerator;

      InitializeStores();
    }
  }
}