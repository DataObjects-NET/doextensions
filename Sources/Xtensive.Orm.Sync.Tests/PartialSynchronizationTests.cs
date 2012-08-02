using System;
using System.Threading;
using Microsoft.Synchronization;
using NUnit.Framework;
using Xtensive.Orm.Sync.Tests.Model;

namespace Xtensive.Orm.Sync.Tests
{
  [TestFixture]
  public class PartialSynchronizationTests : AutoBuildTest
  {
    public override void TestFixtureSetUp()
    {
    }

    public override void TestFixtureTearDown()
    {
    }

    public override void TestSetUp()
    {
      base.TestFixtureTearDown();
      base.TestFixtureSetUp();

      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {

          for (int i = 0; i < 20; i++) {
            new MyEntity(session) {
              Property = new MyReferenceProperty(session)
            };
          }
          t.Complete();
        }
      }
    }

    [Test]
    public void SyncStandaloneEntitiesTest()
    {
      Thread.Sleep(TimeSpan.FromSeconds(2));
      var localProvider = LocalDomain.GetSyncProvider();
      localProvider.Sync.All<MyReferenceProperty>();
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
          t.Complete();
        }
      }
    }
    [Test]
    public void SyncReferencingEntitiesTest()
    {
      Thread.Sleep(TimeSpan.FromSeconds(2));
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
  }
}
