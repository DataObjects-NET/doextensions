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

    public IEnumerable<MetadataQueryGroup> GetQueryGroups(SyncKnowledge destKnowledge)
    {
      foreach (var store in manager.GetStores(configuration)) {
        var result = new MetadataQueryGroup(store, GetFilter(store.EntityType));
        var knowledge = destKnowledge.GetKnowledgeForRange(store.MinItemId, store.MaxItemId);
        BuildQueriesForStore(store, knowledge, result);
        if (result.Count==0)
          result.Add(new MetadataQuery());
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
      var minIdValue = minId.ToString();
      var maxIdValue = maxId.ToString();

      if (clockVector.Count==0) {
        output.Add(new MetadataQuery(minIdValue, maxIdValue));
        return;
      }

      foreach (var item in clockVector) {
        var lastKnownVersion = new SyncVersion(item.ReplicaKey, item.TickCount);
        output.Add(new MetadataQuery(minIdValue, maxIdValue, lastKnownVersion));
      }

      var knownReplicas = clockVector.Select(item => item.ReplicaKey);
      output.Add(new MetadataQuery(minIdValue, maxIdValue, replicasToExclude: knownReplicas));
    }

    private Expression GetFilter(Type type)
    {
      Expression result;
      configuration.Filters.TryGetValue(type, out result);
      return result;
    }

    public MetadataQueryBuilder(MetadataManager manager, SyncConfiguration configuration)
    {
      if (manager==null)
        throw new ArgumentNullException("manager");
      if (configuration==null)
        throw new ArgumentNullException("configuration");

      this.manager = manager;
      this.configuration = configuration;
    }
  }
}