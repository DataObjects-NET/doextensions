using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  internal sealed class MetadataQueryTaskBuilder
  {
    private readonly MetadataManager manager;
    private readonly SyncConfiguration configuration;

    public IEnumerable<MetadataQueryTask> GetTasks(SyncKnowledge destKnowledge)
    {
      var rangeQueries = BuildRangeQueries(destKnowledge);

      foreach (var store in manager.GetStores(configuration)) {
        List<MetadataQueryTask> tasks;
        if (rangeQueries.TryGetValue(store, out tasks))
          foreach (var task in tasks)
            yield return task;
        else
          yield return new MetadataQueryTask(store, GetFilter(store.EntityType));
      }
    }

    private Dictionary<MetadataStore, List<MetadataQueryTask>> BuildRangeQueries(SyncKnowledge destKnowledge)
    {
      var result = new Dictionary<MetadataStore, List<MetadataQueryTask>>();

      Range currentRange = null;

      foreach (var nextRange in new KnowledgeFragmentInspector(destKnowledge).ScopeRangeSet) {
        if (currentRange!=null)
          MakeTaskFromRange(currentRange, nextRange, result);
        currentRange = nextRange;
      }

      MakeTaskFromRange(currentRange, null, result);

      return result;
    }

    private void MakeTaskFromRange(Range currentRange, Range nextRange, Dictionary<MetadataStore, List<MetadataQueryTask>> output)
    {
      var hierarchyId = SyncIdFormatter.GetInfo(currentRange.ItemId).HierarchyId;
      if (hierarchyId==0)
        return;

      var store = manager.GetStore(hierarchyId);
      var minId = currentRange.ItemId.ToString();
      var maxId = (nextRange!=null ? nextRange.ItemId : SyncIdFormatter.MaxId).ToString();
      var lastKnownVersions = currentRange.ClockVector.Select(item => new SyncVersion(item.ReplicaKey, item.TickCount));

      var task = new MetadataQueryTask(store, minId, maxId, lastKnownVersions, GetFilter(store.EntityType));

      List<MetadataQueryTask> taskList;
      if (!output.TryGetValue(store, out taskList)) {
        taskList = new List<MetadataQueryTask>();
        output.Add(store, taskList);
      }
      taskList.Add(task);
    }

    private Expression GetFilter(Type type)
    {
      Expression result;
      configuration.Filters.TryGetValue(type, out result);
      return result;
    }

    public MetadataQueryTaskBuilder(MetadataManager manager, SyncConfiguration configuration)
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