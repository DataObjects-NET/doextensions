using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;
using Xtensive.IoC;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (MetadataUpdater), Singleton = true)]
  internal sealed class MetadataUpdater : ISessionService
  {
    private readonly Session session;
    private readonly MetadataManager metadataManager;

    public void WriteSyncLog(IEnumerable<EntityChangeInfo> changes)
    {
      foreach (var item in changes)
        new SyncLog(session, item.Key, item.ChangeKind);
    }

    public void UpdateMetadata(ICollection<EntityChangeInfo> changes)
    {
      var metadataSet = metadataManager.GetMetadata(changes.Select(i => i.Key));
      foreach (var item in changes) {
        var metadata = metadataSet[item.Key];
        if (metadata==null)
          metadataManager.CreateMetadata(item.Key);
        else
          metadataManager.UpdateMetadata(metadata, item.ChangeKind==EntityChangeKind.Remove);
      }
    }

    public bool MaintainSyncLogOnce()
    {
      var logItems = GetNextSyncLogBatch();
      if (logItems.Count==0)
        return false;

      var metadataSet = metadataManager.GetMetadata(logItems.Select(log => log.TargetKey.Key));
      foreach (var item in logItems) {
        var metadata = metadataSet[item.TargetKey.Key];
        if (metadata==null)
          metadataManager.CreateMetadata(item.TargetKey.Key, item.Tick);
        else
          metadataManager.UpdateMetadata(metadata, item.ChangeKind==EntityChangeKind.Remove, item.Tick);
        item.Remove();
      }

      return true;
    }

    public static bool MaintainSyncLogOnce(Domain domain)
    {
      bool resume;

      using (var session = domain.OpenSession(WellKnown.SyncSessionConfiguration))
      using (var tx = session.OpenTransaction()) {
        var updater = session.Services.Demand<MetadataUpdater>();
        resume = updater.MaintainSyncLogOnce();
        tx.Complete();
      }

      return resume;
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