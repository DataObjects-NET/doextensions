using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.IoC;
using Xtensive.Orm.Tracking;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (MetadataUpdater), Singleton = true)]
  internal sealed class MetadataUpdater : ISessionService
  {
    private readonly Session session;
    private readonly MetadataManager metadataManager;

    public void WriteSyncLog(IEnumerable<ITrackingItem> trackingItems)
    {
      foreach (var item in trackingItems)
        new SyncLog(session, item.Key, item.State);
    }

    public void UpdateMetadata(ICollection<ITrackingItem> trackingItems)
    {
      var metadataSet = metadataManager.GetMetadata(trackingItems.Select(i => i.Key));
      foreach (var item in trackingItems) {
        var metadata = metadataSet[item.Key];
        if (metadata==null)
          metadataManager.CreateMetadata(item.Key);
        else
          metadataManager.UpdateMetadata(metadata, item.State==TrackingItemState.Deleted);
      }
    }

    public bool MaintainSyncLogOnce()
    {
      var logItems = GetNextSyncLogBatch();
      if (logItems.Count==0)
        return false;

      var metadataSet = metadataManager.GetMetadata(logItems.Select(log => log.EntityKey));
      foreach (var item in logItems) {
        var metadata = metadataSet[item.EntityKey];
        if (metadata==null)
          metadataManager.CreateMetadata(item.EntityKey, item.Tick);
        else
          metadataManager.UpdateMetadata(metadata, item.ChangeKind==TrackingItemState.Deleted, item.Tick);
        item.Remove();
      }

      return true;
    }

    private List<SyncLog> GetNextSyncLogBatch()
    {
      return session.Query
        .Execute(q => q.All<SyncLog>().OrderBy(log => log.Tick).Take(() => WellKnown.SyncLogBatchSize))
        .ToList();
    }

    [ServiceConstructor]
    public MetadataUpdater(Session session, MetadataManager metadataManager)
    {
      if (session==null)
        throw new ArgumentNullException("session");
      if (metadataManager==null)
        throw new ArgumentNullException("metadataManager");

      this.session = session;
      this.metadataManager = metadataManager;
    }
  }
}