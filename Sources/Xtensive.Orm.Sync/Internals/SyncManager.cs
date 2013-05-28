using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Synchronization;
using Xtensive.Core;
using Xtensive.IoC;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Providers;
using Xtensive.Orm.Services;
using Xtensive.Orm.Sync.Model;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (ISyncManager), Singleton = true)]
  internal sealed class SyncManager : ISyncManager, IDomainService, IDisposable
  {
    private readonly Domain domain;
    private readonly HashSet<Type> synchronizedRoots;
    private readonly bool directMetadataUpdates;

    private MetadataProcessor processor;

    public SyncId ReplicaId { get; private set; }
    public IList<Type> SynchronizedRoots { get; private set; }

    public OrmSyncProvider GetSyncProvider()
    {
      return new OrmSyncProvider(domain);
    }

    public bool UpdateMetadataOnce()
    {
      return MetadataUpdater.MaintainSyncLogOnce(domain);
    }

    public void UpdateMetadata()
    {
      while (MetadataUpdater.MaintainSyncLogOnce(domain)) {
        // do nothing
      }
    }

    public void WaitForPendingSyncTasks()
    {
      if (processor==null)
        return;
      processor.WaitForIdle();
    }

    public void StartMetadataProcessor()
    {
      if (directMetadataUpdates || processor!=null)
        return;

      processor = new MetadataProcessor(domain);
    }

    public bool IsSyncRunning(Session session)
    {
      if (session==null)
        throw new ArgumentNullException("session");
      return SyncSessionMarker.Check(session);
    }

    public void ForgetMetadata(Session session, Type type)
    {
      if (session==null)
        throw new ArgumentNullException("session");
      if (type==null)
        throw new ArgumentNullException("type");
      if (!synchronizedRoots.Contains(type))
        throw UnknownHierarchyRoot(type);

      if (session.Transaction!=null)
        session.SaveChanges();

      using (SyncSessionMarker.Add(session))
      using (var tx = session.OpenTransaction()) {
        var manager = session.Services.Demand<MetadataManager>();
        var store = manager.GetStore(domain.Model.Types[type]);
        store.ForgetMetadata();
        tx.Complete();
      }
    }

    public void CreateMissingMetadata(Session session, Type type)
    {
      if (session==null)
        throw new ArgumentNullException("session");
      if (type==null)
        throw new ArgumentNullException("type");
      if (!synchronizedRoots.Contains(type))
        throw UnknownHierarchyRoot(type);

      if (session.Transaction!=null)
        session.SaveChanges();

      using (SyncSessionMarker.Add(session))
      using (var tx = session.OpenTransaction()) {
        var manager = session.Services.Demand<MetadataManager>();
        var store = manager.GetStore(domain.Model.Types[type]);
        store.CreateMissingMetadata();
        tx.Complete();
      }
    }

    public void Dispose()
    {
      if (processor==null)
        return;
      processor.Dispose();
      processor = null;
    }

    private ArgumentException UnknownHierarchyRoot(Type type)
    {
      return new ArgumentException(string.Format("Type '{0}' is not synchronized hierarchy root", type.FullName), "type");
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
      session.Events.TransactionCommitted += OnTransactionCommitted;
    }

    private void OnTransactionCommitted(object sender, TransactionEventArgs e)
    {
      if (e.Transaction.IsNested || SyncSessionMarker.Check(e.Transaction.Session) || processor==null)
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
        if (directMetadataUpdates)
          updater.UpdateMetadata(items);
        else
          updater.WriteSyncLog(items);
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
        ReplicaId = session.Services.Demand<ReplicaInfoManager>().LoadReplicaId();
        tx.Complete();
      }
    }

    private bool IsUserEntity(EntityState state)
    {
      var rootType = state.Entity.TypeInfo.Hierarchy.Root.UnderlyingType;
      return synchronizedRoots.Contains(rootType);
    }

    [ServiceConstructor]
    public SyncManager(Domain domain)
    {
      if (domain==null)
        throw new ArgumentNullException("domain");

      this.domain = domain;
      directMetadataUpdates = !domain.StorageProviderInfo.Supports(ProviderFeatures.SingleSessionAccess);

      synchronizedRoots = domain.Model.Types[typeof (SyncInfo)]
        .GetDescendants()
        .Select(t => t.UnderlyingType.GetGenericArguments()[0])
        .ToHashSet();

      SynchronizedRoots = synchronizedRoots.ToList().AsReadOnly();

      SubscribeToDomainEvents();
      LoadReplicaId();
    }
  }
}