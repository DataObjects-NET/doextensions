using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Synchronization;
using Xtensive.Collections.Graphs;
using Xtensive.IoC;
using Xtensive.Orm.Model;
using Xtensive.Orm.Sync.Model;
using FieldInfo = Xtensive.Orm.Model.FieldInfo;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (MetadataManager), Singleton = true)]
  internal sealed class MetadataManager : ISessionService
  {
    private readonly SyncId replicaId;
    private readonly SyncTickGenerator tickGenerator;
    private readonly HierarchyIdRegistry hierarchyIdRegistry;
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

    public MetadataSet GetMetadata(IEnumerable<Key> keys)
    {
      var result = new MetadataSet();
      foreach (var item in GetMetadataItems(keys))
        result.Add(item);
      return result;
    }

    private IEnumerable<SyncInfo> GetMetadataItems(IEnumerable<Key> keys)
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

    public SyncInfo CreateMetadata(Key key, long tick = -1)
    {
      var store = GetStore(key.TypeInfo);
      var hiearchyId = hierarchyIdRegistry.GetHierarchyId(key.TypeInfo);

      if (tick < 0)
        tick = tickGenerator.GetNextTick();

      var syncId = SyncIdBuilder.GetSyncId(hiearchyId, replicaId, tick);
      var result = store.CreateMetadata(syncId, key);

      result.CreationVersion = GetLocalVersion(tick);
      result.ChangeVersion = GetLocalVersion(tick);

      return result;
    }

    public SyncInfo CreateMetadata(Key key, ItemChange change)
    {
      var store = GetStore(key.TypeInfo);
      var result = store.CreateMetadata(change.ItemId, key);
      result.CreationVersion = GetVersion(change.CreationVersion);
      result.ChangeVersion = GetVersion(change.ChangeVersion);
      return result;
    }

    public void UpdateMetadata(SyncInfo item, bool markAsTombstone, long tick = -1)
    {
      if (tick < 0)
        tick = tickGenerator.GetNextTick();

      item.ChangeVersion = GetLocalVersion(tick);

      if (markAsTombstone)
        item.IsTombstone = true;
    }

    public void UpdateMetadata(SyncInfo item, ItemChange change, bool markAsTombstone)
    {
      item.ChangeVersion = GetVersion(change.ChangeVersion);

      if (markAsTombstone)
        item.IsTombstone = true;
    }

    private SyncVersionData GetVersion(SyncVersion version)
    {
      return new SyncVersionData(session, version);
    }

    private SyncVersionData GetLocalVersion(long tick)
    {
      return new SyncVersionData(session, WellKnown.LocalReplicaKey, tick);
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
    public MetadataManager(Session session, SyncTickGenerator tickGenerator, HierarchyIdRegistry hierarchyIdRegistry, ISyncManager syncManager)
    {
      if (session==null)
        throw new ArgumentNullException("session");
      if (tickGenerator==null)
        throw new ArgumentNullException("tickGenerator");
      if (hierarchyIdRegistry==null)
        throw new ArgumentNullException("hierarchyIdRegistry");
      if (syncManager==null)
        throw new ArgumentNullException("syncManager");

      this.session = session;
      this.tickGenerator = tickGenerator;
      this.hierarchyIdRegistry = hierarchyIdRegistry;

      replicaId = syncManager.ReplicaId;

      InitializeStores();
    }
  }
}