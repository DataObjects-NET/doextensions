using System;
using System.Linq;
using Microsoft.Synchronization;
using Xtensive.Core;
using Xtensive.IoC;
using Xtensive.Orm.Providers;
using Xtensive.Orm.Tracking;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (ISyncManager), Singleton = true)]
  internal sealed class SyncManager : ISyncManager, IDomainService, IDisposable
  {
    private readonly Domain domain;
    private readonly bool useSyncLog;

    private MetadataProcessor processor;

    public SyncId ReplicaId { get; private set; }

    public OrmSyncProvider GetSyncProvider()
    {
      return new OrmSyncProvider(domain);
    }

    public void WaitForPendingSyncTasks()
    {
      if (processor==null)
        return;
      processor.WaitForIdle();
    }

    public void StartMetadataProcessor()
    {
      if (!useSyncLog || processor!=null)
        return;

      processor = new MetadataProcessor(domain);
    }

    public void Initialize()
    {
      SubscribeToTrackingEvents();
      LoadReplicaId();
    }

    public void Dispose()
    {
      if (processor==null)
        return;
      processor.Dispose();
      processor = null;
    }

    private void SubscribeToTrackingEvents()
    {
      var trackingMonitor = domain.GetTrackingMonitor();
      if (trackingMonitor==null)
        throw new InvalidOperationException(
          "Tracking monitor is not enabled in this domain, register Xtensive.Orm.Tracking assembly in DomainConfiguration");
      trackingMonitor.TrackingCompleted += OnTrackingCompleted;
    }

    private void LoadReplicaId()
    {
      using (var session = domain.OpenSession(WellKnown.SyncSessionConfiguration))
      using (var tx = session.OpenTransaction()) {
        ReplicaId = session.Services.Demand<ReplicaManager>().LoadReplicaId();
        tx.Complete();
      }
    }

    private void OnTrackingCompleted(object sender, TrackingCompletedEventArgs e)
    {
      var items = e.Changes.Where(IsValidTrackingItem).ToList();
      if (items.Count==0)
        return;

      using (var tx = e.Session.OpenTransaction()) {
        var updater = e.Session.Services.Demand<MetadataUpdater>();
        if (useSyncLog)
          updater.WriteSyncLog(items);
        else
          updater.UpdateMetadata(items);
        tx.Complete();
      }

      if (processor!=null)
        processor.NotifyDataAvailable();
    }

    private static bool IsValidTrackingItem(ITrackingItem item)
    {
      var entityKey = item.Key;
      var entityType = entityKey.TypeInfo.UnderlyingType;

      if (entityType.Assembly==typeof (Persistent).Assembly)
        return false;
      if (entityType.Assembly==typeof (SyncInfo).Assembly)
        return false;
      if (entityKey.TypeInfo.IsAuxiliary)
        return false;

      return true;
    }

    [ServiceConstructor]
    public SyncManager(Domain domain)
    {
      if (domain==null)
        throw new ArgumentNullException("domain");

      this.domain = domain;
      useSyncLog = !domain.StorageProviderInfo.Supports(ProviderFeatures.SingleSessionAccess);
    }
  }
}