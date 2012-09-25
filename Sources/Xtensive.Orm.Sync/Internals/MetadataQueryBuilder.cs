using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  internal sealed class MetadataQueryBuilder
  {
    private readonly MetadataManager manager;
    private readonly SyncConfiguration configuration;
    private readonly HashSet<uint> locallyKnownReplicas;

    public IEnumerable<MetadataQueryGroup> GetQueryGroups(SyncKnowledge destKnowledge)
    {
      foreach (var store in manager.GetStores(configuration)) {
        var result = new MetadataQueryGroup(store, GetFilter(store.EntityType));
        var knowledge = destKnowledge.GetKnowledgeForRange(store.MinItemId, store.MaxItemId);
        BuildQueriesForStore(store, knowledge, result);
        yield return result;
      }
    }

    private void BuildQueriesForStore(MetadataStore store, SyncKnowledge knowledge, MetadataQueryGroup output)
    {
      Range currentRange = null;

      var scopeRangeSet = new KnowledgeFragmentInspector(knowledge).ScopeRangeSet;
      foreach (var nextRange in scopeRangeSet) {
        if (currentRange!=null) {
          var minId = currentRange.ItemId;
          var maxId = nextRange.ItemId;
          if (maxId > store.MinItemId)
            BuildQueriesForRange(minId, maxId, currentRange.ClockVector, output);
        }
        currentRange = nextRange;
      }

      if (currentRange!=null && currentRange.ItemId < store.MaxItemId) {
        var minId = currentRange.ItemId;
        var maxId = store.MaxItemId;
        if (minId < store.MinItemId)
          minId = store.MinItemId;
        BuildQueriesForRange(minId, maxId, currentRange.ClockVector, output);
      }
    }

    private void BuildQueriesForRange(SyncId minId, SyncId maxId, IClockVector clockVector, MetadataQueryGroup output)
    {
      if (clockVector.Count==0) {
        output.Add(new MetadataQuery(minId, maxId));
        return;
      }

      var knownReplicas = new HashSet<uint>();
      var unknownReplicas = new HashSet<uint>(locallyKnownReplicas);

      foreach (var item in clockVector) {
        var replicaKey = item.ReplicaKey;
        output.Add(new MetadataQuery(minId, maxId, replicaKey, (long) item.TickCount));
        knownReplicas.Add(replicaKey);
        unknownReplicas.Remove(replicaKey);
      }

      foreach (var replicaKey in unknownReplicas)
        output.Add(new MetadataQuery(minId, maxId, replicaKey));
    }

    private Expression GetFilter(Type type)
    {
      Expression result;
      configuration.Filters.TryGetValue(type, out result);
      return result;
    }

    public MetadataQueryBuilder(ReplicaInfo replicaInfo, SyncConfiguration configuration, MetadataManager manager)
    {
      if (replicaInfo==null)
        throw new ArgumentNullException("replicaInfo");
      if (configuration==null)
        throw new ArgumentNullException("configuration");
      if (manager == null)
        throw new ArgumentNullException("manager");

      this.manager = manager;
      this.configuration = configuration;

      locallyKnownReplicas = replicaInfo.GetKnownReplicas();
    }
  }
}