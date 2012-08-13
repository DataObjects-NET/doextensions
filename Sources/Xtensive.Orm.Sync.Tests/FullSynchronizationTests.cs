using System;
using System.Linq;
using System.Threading;
using Microsoft.Synchronization;
using NUnit.Framework;
using Xtensive.Orm.Sync.Tests.Model;

namespace Xtensive.Orm.Sync.Tests
{
  [TestFixture]
  public class FullSynchronizationTests : AutoBuildTest
  {

    public override void TestSetUp()
    {
      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {

          for (int i = 0; i < 20; i++) {
            new MyEntity(session) {
              Property = new MyReferenceProperty(session)
            };
            new AnotherEntity(session, Guid.NewGuid());
          }

          t.Complete();
        }
      }
      Thread.Sleep(TimeSpan.FromSeconds(2));
      var orchestrator = new SyncOrchestrator {
          LocalProvider = LocalDomain.GetSyncProvider(),
          RemoteProvider = RemoteDomain.GetSyncProvider(),
          Direction = SyncDirectionOrder.Upload
        };
      orchestrator.Synchronize();
    }

    [Test]
    public void CreateSyncTest()
    {
      int myEntityCount, myReferencePropertyCount, syncInfoCount;
      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          myEntityCount = session.Query.All<MyEntity>().Count();
          myReferencePropertyCount = session.Query.All<MyReferenceProperty>().Count();
          syncInfoCount = session.Query.All<SyncInfo>().Count();
          t.Complete();
        }
      }

      using (var session = RemoteDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          Assert.AreEqual(myEntityCount, session.Query.All<MyEntity>().Count());
          Assert.AreEqual(myReferencePropertyCount, session.Query.All<MyReferenceProperty>().Count());
          Assert.AreEqual(0, session.Query.All<MyEntity>().Count(m => m.Property == null));
          Assert.AreEqual(syncInfoCount, session.Query.All<SyncInfo>().Count());
          t.Complete();
        }
      }
    }

    [Test]
    public void CreateSyncUpdateSyncTest()
    {
      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {

          foreach (var entity in session.Query.All<MyEntity>()) {
            entity.Date = DateTime.MaxValue;
          }
          t.Complete();
        }
      }

      Thread.Sleep(TimeSpan.FromSeconds(2));
      var orchestrator = new SyncOrchestrator {
          LocalProvider = LocalDomain.GetSyncProvider(),
          RemoteProvider = RemoteDomain.GetSyncProvider(),
          Direction = SyncDirectionOrder.Upload
        };
      orchestrator.Synchronize();

      int myEntityCount, myReferencePropertyCount, syncInfoCount;
      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          myEntityCount = session.Query.All<MyEntity>().Count();
          myReferencePropertyCount = session.Query.All<MyReferenceProperty>().Count();
          syncInfoCount = session.Query.All<SyncInfo>().Count();
          t.Complete();
        }
      }

      using (var session = RemoteDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          Assert.AreEqual(myEntityCount, session.Query.All<MyEntity>().Count());
          Assert.AreEqual(myReferencePropertyCount, session.Query.All<MyReferenceProperty>().Count());
          Assert.AreEqual(0, session.Query.All<MyEntity>().Count(m => m.Date != DateTime.MaxValue));
          Assert.AreEqual(syncInfoCount, session.Query.All<SyncInfo>().Count());
          t.Complete();
        }
      }
    }

    [Test]
    public void CreateSyncRemoveSyncTest()
    {
      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {

          foreach (var entity in session.Query.All<MyEntity>().ToList()) {
            entity.Remove();
          }
          t.Complete();
        }
      }

      Thread.Sleep(TimeSpan.FromSeconds(2));
      var orchestrator = new SyncOrchestrator {
          LocalProvider = LocalDomain.GetSyncProvider(),
          RemoteProvider = RemoteDomain.GetSyncProvider(),
          Direction = SyncDirectionOrder.Upload
        };
      orchestrator.Synchronize();

      int myEntityCount, myReferencePropertyCount, syncInfoCount;
      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          myEntityCount = session.Query.All<MyEntity>().Count();
          myReferencePropertyCount = session.Query.All<MyReferenceProperty>().Count();
          syncInfoCount = session.Query.All<SyncInfo>().Count();
          t.Complete();
        }
      }

      using (var session = RemoteDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          Assert.AreEqual(myEntityCount, session.Query.All<MyEntity>().Count());
          Assert.AreEqual(myReferencePropertyCount, session.Query.All<MyReferenceProperty>().Count());
          Assert.AreEqual(syncInfoCount, session.Query.All<SyncInfo>().Count());
          t.Complete();
        }
      }
    }
  }
}
