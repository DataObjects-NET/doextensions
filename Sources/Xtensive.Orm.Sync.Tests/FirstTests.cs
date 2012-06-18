using System;
using Microsoft.Synchronization;
using NUnit.Framework;
using Xtensive.Orm.Sync.Tests.Model;

namespace Xtensive.Orm.Sync.Tests
{
  [TestFixture]
  public class FirstTests : AutoBuildTest
  {
    [Test]
    public void CreateSyncTest()
    {
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

      var orchestrator = new SyncOrchestrator {
          LocalProvider = LocalDomain.Services.Get<SyncProvider>(),
          RemoteProvider = RemoteDomain.Services.Get<SyncProvider>(),
          Direction = SyncDirectionOrder.Upload
        };
      orchestrator.Synchronize();
    }

    [Test]
    public void CreateSyncUpdateSyncTest()
    {
      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          for (int i = 0; i < 2; i++) {
            new MyEntity(session) {
              Property = new MyReferenceProperty(session)
            };
          }

          t.Complete();
        }
      }

      var orchestrator = new SyncOrchestrator {
          LocalProvider = LocalDomain.Services.Get<SyncProvider>(),
          RemoteProvider = RemoteDomain.Services.Get<SyncProvider>(),
          Direction = SyncDirectionOrder.Upload
        };
      orchestrator.Synchronize();

      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {

          foreach (var entity in session.Query.All<MyEntity>()) {
            entity.Date = DateTime.Now;
          }
          t.Complete();
        }
      }

      orchestrator = new SyncOrchestrator {
          LocalProvider = LocalDomain.Services.Get<SyncProvider>(),
          RemoteProvider = RemoteDomain.Services.Get<SyncProvider>(),
          Direction = SyncDirectionOrder.Upload
        };
      orchestrator.Synchronize();
    }
  }
}
