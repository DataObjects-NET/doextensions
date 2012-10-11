using NUnit.Framework;

namespace Xtensive.Orm.Sync.Tests
{
  [TestFixture]
  public class SyncModuleTests : SingleSyncTest
  {
    [Test]
    public void InfrastructureTest()
    {
      var syncManager1 = LocalDomain.Services.Get<ISyncManager>();
      Assert.IsNotNull(syncManager1);

      var syncManager2 = LocalDomain.GetSyncManager();
      Assert.IsNotNull(syncManager1);
      
      Assert.AreSame(syncManager1, syncManager2);
    }

    [Test]
    public void ReplicaIdTest()
    {
      var syncId = LocalDomain.GetSyncManager().ReplicaId;
      Assert.That(syncId, Is.Not.Null);
    }
  }
}
