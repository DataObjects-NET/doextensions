using System;
using System.Linq;
using Microsoft.Synchronization;
using Xtensive.Core;
using Xtensive.IoC;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Providers;
using Xtensive.Orm.Services;

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

    public bool IsSyncRunning(Session session)
    {
      return SyncSessionMarker.Check(session);
    }

    public void Initialize()
    {
      SubscribeToDomainEvents();
      LoadReplicaId();
    }

    public void Dispose()
    {
      if (processor==null)
        return;
      processor.Dispose();
      processor = null;
    }

    private void SubscribeToDomainEvents()
    {
      domain.SessionOpen += OnSessionOpen;
    }

    private void OnSessionOpen(object sender, SessionEventArgs e)
    {
      var session = e.Session;
      if (session.Configuration.Type!=SessionType.User)
        return;
      session.Events.Persisting += OnPersisting;
      if (processor!=null)
        session.Events.TransactionCommitted += OnTransactionCommitted;
    }

    private void OnTransactionCommitted(object sender, TransactionEventArgs e)
    {
      if (e.Transaction.IsNested || SyncSessionMarker.Check(e.Transaction.Session))
        return;
      processor.NotifyDataAvailable();
    }

    private void OnPersisting(object sender, EventArgs e)
    {
      var session = ((SessionEventAccessor) sender).Session;
      if (SyncSessionMarker.Check(session))
        return;
      var accessor = session.Services.Demand<DirectSessionAccessor>();

      var items = accessor.GetChangedEntities(PersistenceState.Removed)
        .Concat(accessor.GetChangedEntities(PersistenceState.Modified))
        .Concat(accessor.GetChangedEntities(PersistenceState.New))
        .Where(IsUserEntity)
        .Select(s => new EntityChangeInfo(s.Key, GetChangeKind(s.PersistenceState)))
        .ToList();

      if (items.Count==0)
        return;

      using (session.DisableSaveChanges()) {
        var updater = session.Services.Demand<MetadataUpdater>();
        if (useSyncLog)
          updater.WriteSyncLog(items);
        else
          updater.UpdateMetadata(items);
      }
    }

    private EntityChangeKind GetChangeKind(PersistenceState persistenceState)
    {
      switch (persistenceState) {
        case PersistenceState.New:
          return EntityChangeKind.Create;
        case PersistenceState.Modified:
          return EntityChangeKind.Update;
        case PersistenceState.Removed:
          return EntityChangeKind.Remove;
        default:
          throw new ArgumentOutOfRangeException("persistenceState");
      }
    }

    private void LoadReplicaId()
    {
      using (var session = domain.OpenSession(WellKnown.SyncSessionConfiguration))
      using (var tx = session.OpenTransaction()) {
        ReplicaId = session.Services.Demand<ReplicaManager>().LoadReplicaId();
        tx.Complete();
      }
    }

    private static bool IsUserEntity(EntityState state)
    {
      var entityType = state.Entity.GetType();

      if (entityType.Assembly==typeof (Persistent).Assembly)
        return false;
      if (entityType.Assembly==typeof (SyncInfo).Assembly)
        return false;
      if (state.Type.IsAuxiliary)
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