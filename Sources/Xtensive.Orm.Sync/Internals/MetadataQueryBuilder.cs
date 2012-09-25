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
        if (currentRange!=null)
          BuildQueriesForRange(currentRange, nextRange.ItemId, output);
        currentRange = nextRange;
      }

      BuildQueriesForRange(currentRange, store.MaxItemId, output);
    }

    private void BuildQueriesForRange(Range currentRange, SyncId upperBound, MetadataQueryGroup output)
    {
      var minId = currentRange.ItemId.ToString();
      var maxId = upperBound.ToString();

      if (currentRange.ClockVector.Count==0) {
        output.Add(new MetadataQuery(minId, maxId));
        return;
      }

      foreach (var item in currentRange.ClockVector) {
        var lastKnownVersion = new SyncVersion(item.ReplicaKey, item.TickCount);
        output.Add(new MetadataQuery(minId, maxId, lastKnownVersion));
      }

      var knownReplicas = currentRange.ClockVector.Select(item => item.ReplicaKey);
      output.Add(new MetadataQuery(minId, maxId, replicasToExclude: knownReplicas));
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