using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Synchronization;
using NUnit.Framework;
using Xtensive.Orm.Sync.Tests.Model;

namespace Xtensive.Orm.Sync.Tests
{
  [TestFixture]
  public class FullSynchronizationTests : MultisyncTest
  {
    private const int InitialEntityCount = 300;
    private const int AdditionalEntityCount = 300;

    private int myEntityCount, myReferencePropertyCount, syncInfoCount;

    public override void TestSetUp()
    {
      base.TestSetUp();

      CreateEntitiesInLocalDomain(InitialEntityCount);
      SynchronizeLocalToRemote();
    }

    [Test]
    public void CreateSyncTest()
    {
      // Validate
      FetchLocalEntityCount();
      using (var session = RemoteDomain.OpenSession())
      using (session.OpenTransaction()) {
        ValidateEntityCount(session);
        Assert.AreEqual(0, Count<MyEntity>(session, m => m.Property==null));
      }
    }

    [Test]
    public void CreateAndCreateSyncTest()
    {
      for (int i = 0; i < 1; i++) {

        // Modify
        CreateEntitiesInLocalDomain(AdditionalEntityCount);

        // Synchronize
        SynchronizeLocalToRemote();

      }

      // Validate
      FetchLocalEntityCount();
      using (var session = RemoteDomain.OpenSession())
      using (session.OpenTransaction()) {
        ValidateEntityCount(session);
      }
    }

    [Test]
    public void CreateSyncUpdateSyncTest()
    {
      // Modify
      using (var session = LocalDomain.OpenSession())
      using (var t = session.OpenTransaction()) {
        foreach (var entity in session.Query.All<MyEntity>())
          entity.Date = DateTime.MaxValue;
        t.Complete();
      }
      LocalDomain.WaitForPendingSyncTasks();

      // Synchronize
      SynchronizeLocalToRemote();

      // Validate
      FetchLocalEntityCount();
      using (var session = RemoteDomain.OpenSession())
      using (var t = session.OpenTransaction()) {
        ValidateEntityCount(session);
        Assert.AreEqual(0, Count<MyEntity>(session, m => m.Date!=DateTime.MaxValue));
      }
    }

    [Test]
    public void CreateSyncRemoveSyncTest()
    {
      // Modify
      using (var session = LocalDomain.OpenSession())
      using (var t = session.OpenTransaction()) {
        foreach (var entity in session.Query.All<MyEntity>().ToList())
          entity.Remove();
        t.Complete();
      }
      LocalDomain.WaitForPendingSyncTasks();

      // Synchronize
      SynchronizeLocalToRemote();

      // Validate
      FetchLocalEntityCount();
      using (var session = RemoteDomain.OpenSession())
      using (session.OpenTransaction()) {
        ValidateEntityCount(session);
      }
    }

    private void FetchLocalEntityCount()
    {
      using (var session = LocalDomain.OpenSession())
      using (var t = session.OpenTransaction()) {
        myEntityCount = Count<MyEntity>(session);
        myReferencePropertyCount = Count<MyReferenceProperty>(session);
        syncInfoCount = Count<SyncInfo>(session);
        t.Complete();
      }
    }

    private void ValidateEntityCount(Session session)
    {
      Assert.AreEqual(myEntityCount, Count<MyEntity>(session), "MyEntity count differs");
      Assert.AreEqual(myReferencePropertyCount, Count<MyReferenceProperty>(session), "MyReferenceProperty count differs");
      Assert.AreEqual(syncInfoCount, Count<SyncInfo>(session), "SyncInfo count differs");
    }

    private int Count<TEntity>(Session session, Expression<Func<TEntity, bool>> filter = null)
      where TEntity : class, IEntity
    {
      return filter==null
        ? session.Query.All<TEntity>().Count()
        : session.Query.All<TEntity>().Count(filter);
    }

    private void SynchronizeLocalToRemote(OrmSyncProvider localProvider = null, OrmSyncProvider remoteProvider = null)
    {
      if (localProvider==null)
        localProvider = LocalDomain.GetSyncProvider();
      if (remoteProvider==null)
        remoteProvider = RemoteDomain.GetSyncProvider();

      var orchestrator = new SyncOrchestrator {
        LocalProvider = localProvider,
        RemoteProvider = remoteProvider,
        Direction = SyncDirectionOrder.Upload
      };

      orchestrator.Synchronize();
    }

    private void CreateEntitiesInLocalDomain(int count)
    {
      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          for (int i = 0; i < count; i++) {
            new MyEntity(session) {Property = new MyReferenceProperty(session)};
            new AnotherEntity(session, Guid.NewGuid());
          }
          t.Complete();
        }
      }

      LocalDomain.WaitForPendingSyncTasks();
    }
  }
}
