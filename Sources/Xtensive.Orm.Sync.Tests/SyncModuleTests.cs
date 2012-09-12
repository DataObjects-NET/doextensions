using System.Linq;
using Microsoft.Synchronization;
using NUnit.Framework;
using Xtensive.Orm.Sync.Tests.Model;
using Xtensive.Orm.Tracking;

namespace Xtensive.Orm.Sync.Tests
{
  [TestFixture]
  public class SyncModuleTests : SingleSyncTest
  {
    [Test]
    public void InfrastructureTest()
    {
      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {

          var trackingMonitor = LocalDomain.Services.Get<IDomainTrackingMonitor>();
          Assert.IsNotNull(trackingMonitor);

          var syncManager = LocalDomain.Services.Get<ISyncManager>();
          Assert.IsNotNull(syncManager);
          t.Complete();
        }
      }
    }

    [Test]
    public void TrackingTest()
    {
      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          new MyEntity(session);
          t.Complete();
        }
      }
    }

    [Test]
    public void ReplicaIdTest()
    {
      var syncId = LocalDomain.GetSyncManager().ReplicaId;
      Assert.That(syncId, Is.Not.Null);
    }
  }
}
