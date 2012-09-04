using System.Linq;
using Microsoft.Synchronization;
using NUnit.Framework;
using Xtensive.Orm.Sync.Tests.Model;
using Xtensive.Orm.Tracking;

namespace Xtensive.Orm.Sync.Tests
{
  [TestFixture]
  public class SyncModuleTests : AutoBuildTest
  {
    [Test]
    public void InfrastructureTest()
    {
      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {

          var m = LocalDomain.Services.Get<IDomainTrackingMonitor>();
          Assert.IsNotNull(m);

          var sp = LocalDomain.Services.Get<OrmSyncProvider>();
          Assert.IsNotNull(sp);
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
      var syncId = LocalDomain.GetReplicaId();
      Assert.That(syncId, Is.Not.Null);
    }
  }
}
