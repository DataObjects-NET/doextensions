using Microsoft.Synchronization;
using NUnit.Framework;
using Xtensive.Orm.Sync.Tests.Model;

namespace Xtensive.Orm.Sync.Tests
{
  [TestFixture]
  public class PartialSynchronizationTests : AutoBuildTest
  {
    [Test]
    public void Test()
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

      var localProvider = LocalDomain.GetSyncProvider();
      localProvider.Configuration.Types.Add(typeof(MyReferenceProperty));
      var orchestrator = new SyncOrchestrator {
          LocalProvider = localProvider,
          RemoteProvider = RemoteDomain.GetSyncProvider(),
          Direction = SyncDirectionOrder.Upload
        };
      orchestrator.Synchronize();
      localProvider.Configuration.Types.Clear();
      orchestrator = new SyncOrchestrator {
          LocalProvider = localProvider,
          RemoteProvider = RemoteDomain.GetSyncProvider(),
          Direction = SyncDirectionOrder.Upload
        };
      orchestrator.Synchronize();
    }
  }
}
