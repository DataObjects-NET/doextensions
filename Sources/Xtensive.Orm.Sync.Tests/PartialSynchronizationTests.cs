using System.Linq;
using Microsoft.Synchronization;
using NUnit.Framework;
using Xtensive.Orm.Sync.Tests.Model;

namespace Xtensive.Orm.Sync.Tests
{
  [TestFixture]
  public class PartialSynchronizationTests : MultisyncTest
  {
    public override void TestSetUp()
    {
      base.TestSetUp();

      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {

          for (int i = 0; i < 20; i++) {
            new MyEntity(session) {
              Property = new MyReferenceProperty(session)
            };
            new RegularEntity(session);
          }
          t.Complete();
        }
      }
    }

    [Test]
    public void SyncStandaloneEntitiesTest()
    {
      LocalDomain.WaitForPendingSyncTasks();
      var localProvider = LocalDomain.GetSyncProvider();
      localProvider.Sync.All<MyReferenceProperty>();
      localProvider.Sync.All<AbstractEntity>();
      var orchestrator = new SyncOrchestrator {
          LocalProvider = localProvider,
          RemoteProvider = RemoteDomain.GetSyncProvider(),
          Direction = SyncDirectionOrder.Upload
        };
      orchestrator.Synchronize();

      using (var session = RemoteDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          Assert.AreEqual(0, session.Query.All<MyEntity>().Count());
          Assert.AreEqual(20, session.Query.All<MyReferenceProperty>().Count());
          Assert.AreEqual(20, session.Query.All<AbstractEntity>().Count());
          t.Complete();
        }
      }
    }
    [Test]
    public void SyncReferencingEntitiesTest()
    {
      LocalDomain.WaitForPendingSyncTasks();
      var localProvider = LocalDomain.GetSyncProvider();
      localProvider.Sync.All<MyEntity>();
      var orchestrator = new SyncOrchestrator {
          LocalProvider = localProvider,
          RemoteProvider = RemoteDomain.GetSyncProvider(),
          Direction = SyncDirectionOrder.Upload
        };
      orchestrator.Synchronize();

      using (var session = RemoteDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          Assert.AreEqual(20, session.Query.All<MyEntity>().Count());
          Assert.AreEqual(20, session.Query.All<MyReferenceProperty>().Count());
          t.Complete();
        }
      }
    }

    [Test]
    public void SyncWithFilterTest()
    {
      LocalDomain.WaitForPendingSyncTasks();
      var orchestrator = new SyncOrchestrator {
          LocalProvider = LocalDomain.GetSyncProvider(),
          RemoteProvider = RemoteDomain.GetSyncProvider(),
          Direction = SyncDirectionOrder.Upload
        };
      orchestrator.Synchronize();

      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {

          var items = session.Query.All<MyEntity>()
            .Take(10)
            .ToList();
          items.Remove();
          t.Complete();
        }
      }

      LocalDomain.WaitForPendingSyncTasks();
      var localProvider = LocalDomain.GetSyncProvider();
      localProvider.Sync.All<MyEntity>(e => false);
      orchestrator = new SyncOrchestrator {
          LocalProvider = localProvider,
          RemoteProvider = RemoteDomain.GetSyncProvider(),
          Direction = SyncDirectionOrder.Upload
        };
      orchestrator.Synchronize();

      using (var session = RemoteDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          Assert.AreEqual(10, session.Query.All<MyEntity>().Count());
          Assert.AreEqual(10, session.Query.All<SyncInfo<MyEntity>>()
            .Count(s => s.IsTombstone));
          t.Complete();
        }
      }
    }
  }
}
