using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xtensive.Orm.Building;
using Xtensive.Orm.Building.Definitions;
using Xtensive.Orm.Tracking;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// <see cref="IModule"/> implementation for Sync extension
  /// </summary>
  public class SyncModule : IModule
  {
    private Domain domain;
    private readonly BlockingCollection<List<ITrackingItem>> pendingItems;
    private volatile bool isExecuting;

    /// <summary>
    /// Called when the build of <see cref="T:Xtensive.Orm.Building.Definitions.DomainModelDef"/> is completed.
    /// </summary>
    /// <param name="context">The domain building context.</param>
    /// <param name="model">The domain model definition.</param>
    public void OnDefinitionsBuilt(BuildingContext context, DomainModelDef model)
    {
    }

    /// <summary>
    /// Called when 'complex' build process is completed.
    /// </summary>
    /// <param name="builtDomain">The built domain.</param>
    public void OnBuilt(Domain builtDomain)
    {
      domain = builtDomain;

      domain.Extensions.Set(this);
      domain.Extensions.Set(new EntityTupleFormatterRegistry(domain));

      // Initializing global structures
      using (var session = domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        new Replica(session);
        t.Complete();
      }

      var m = domain.GetTrackingMonitor();
      m.TrackingCompleted += OnTrackingCompleted;
      Task.Factory.StartNew(ProcessQueuedItems, TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning);
    }

    internal bool HasPendingTasks
    {
      get { return isExecuting || pendingItems.Count > 0; }
    }

    private void OnTrackingCompleted(object sender, TrackingCompletedEventArgs e)
    {
      var changes = e.Changes;
      var items = changes
        .Where(TrackingItemFilter)
        .ToList();

      if (items.Count == 0)
        return;

      pendingItems.Add(items);
    }

    private void ProcessQueuedItems()
    {
      using (var session = domain.OpenSession())
        while (!pendingItems.IsCompleted) {
          var items = pendingItems.Take();
          try {
            isExecuting = true;
            using (var t = session.OpenTransaction()) {
              var ms = new Metadata(session, new SyncConfiguration());
              var info = ms.GetMetadata(items.Select(i => i.Key)).ToList();
              var lookup = info
                .ToDictionary(i => i.SyncTargetKey);

              foreach (var item in items) {
                if (item.State==TrackingItemState.Created)
                  ms.CreateMetadata(item.Key);
                else {
                  SyncInfo syncInfo;
                  if (lookup.TryGetValue(item.Key, out syncInfo))
                    ms.UpdateMetadata(syncInfo, item.State==TrackingItemState.Deleted);
                  else
                    ms.CreateMetadata(item.Key);
                }
              }
              t.Complete();
            }
          }
          catch {
            // Log somewhere
          }
          finally {
            isExecuting = false;
          }
        }
    }

    private static bool TrackingItemFilter(ITrackingItem item)
    {
      var entityKey = item.Key;
      var entityType = entityKey.TypeInfo.UnderlyingType;

      if (entityType.Assembly==typeof (Persistent).Assembly)
        return false;
      if (entityType.Assembly==typeof (SyncInfo).Assembly)
        return false;

      return true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncModule"/> class.
    /// </summary>
    public SyncModule()
    {
      pendingItems = new BlockingCollection<List<ITrackingItem>>(new ConcurrentQueue<List<ITrackingItem>>());
    }
  }
}
