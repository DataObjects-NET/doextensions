using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xtensive.Core;
using Xtensive.IoC;
using Xtensive.Orm.Sync.Model;

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
      foreach (var item in changes)
        ProcessMetadataChange(metadataSet, item.Key, item.ChangeKind);
    }

    public bool MaintainSyncLogOnce()
    {
      var logItems = GetNextSyncLogBatch();
      if (logItems.Count==0)
        return false;

      var metadataSet = metadataManager.GetMetadata(logItems.Select(log => log.TargetKey.Key));
      foreach (var item in logItems) {
        ProcessMetadataChange(metadataSet, item.TargetKey.Key, item.ChangeKind, item.Tick);
        item.Remove();
      }

      return true;
    }

    private void ProcessMetadataChange(MetadataSet metadataSet, Key targetKey, EntityChangeKind changeKind, long tick = -1)
    {
      switch (changeKind) {
      case EntityChangeKind.Create:
        var newMetadata = metadataManager.CreateMetadata(targetKey, tick);
        metadataSet.Add(newMetadata);
        break;
      case EntityChangeKind.Update:
      case EntityChangeKind.Remove:
        var existingMetadata = metadataSet[targetKey];
        if (existingMetadata==null)
          throw new InvalidOperationException(string.Format("Metadata for object '{0}' is not found", targetKey.Format()));
        metadataManager.UpdateMetadata(existingMetadata, changeKind==EntityChangeKind.Remove, tick);
        break;
      default:
        throw new ArgumentOutOfRangeException("changeKind");
      }
    }

    public static bool MaintainSyncLogOnce(Domain domain)
    {
      bool resume;

      using (var session = domain.OpenSession(WellKnown.SyncSessionConfiguration))
      using (SyncSessionMarker.Add(session))
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