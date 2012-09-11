using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Synchronization;
using Xtensive.Core;
using Xtensive.Orm.Building;
using Xtensive.Orm.Building.Definitions;
using Xtensive.Orm.Tracking;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// <see cref="IModule"/> implementation for Sync extension.
  /// </summary>
  public sealed class SyncModule : IModule
  {
    private readonly BlockingCollection<List<ITrackingItem>> pendingItems;
    private Domain domain;

    private volatile bool isExecuting;

    internal SyncId ReplicaId { get; private set; }

    internal bool HasPendingTasks
    {
      get { return isExecuting || pendingItems.Count > 0; }
    }

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

      // Initializing global structures

      using (var session = domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        ReplicaId = session.Services.Get<ReplicaManager>().LoadReplicaId();
        t.Complete();
      }

      var trackingMonitor = domain.GetTrackingMonitor();
      trackingMonitor.TrackingCompleted += OnTrackingCompleted;
      Task.Factory.StartNew(ProcessQueuedItems, TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning);
    }

    private void OnTrackingCompleted(object sender, TrackingCompletedEventArgs e)
    {
      var changes = e.Changes;
      var items = changes
        .Where(TrackingItemFilter)
        .ToList();

      if (items.Count==0)
        return;

      pendingItems.Add(items);
    }

    private void ProcessQueuedItems()
    {
      using (var session = domain.OpenSession()) {
        while (!pendingItems.IsCompleted) {
          var items = pendingItems.Take();
          try {
            isExecuting = true;
            using (var t = session.OpenTransaction()) {
              var metadataManager = session.Services.Demand<MetadataManager>();
              metadataManager.LoadReplicaState();
              var lookup = metadataManager.GetMetadata(items.Select(i => i.Key)).ToDictionary(i => i.SyncTargetKey);
              foreach (var item in items) {
                if (item.State==TrackingItemState.Created)
                  metadataManager.CreateMetadata(item.Key);
                else {
                  SyncInfo syncInfo;
                  if (lookup.TryGetValue(item.Key, out syncInfo))
                    metadataManager.UpdateMetadata(syncInfo, item.State==TrackingItemState.Deleted);
                  else
                    metadataManager.CreateMetadata(item.Key);
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
