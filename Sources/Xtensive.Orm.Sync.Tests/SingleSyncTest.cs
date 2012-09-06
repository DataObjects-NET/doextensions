using NUnit.Framework;

namespace Xtensive.Orm.Sync.Tests
{
  [TestFixture]
  public abstract class SingleSyncTest
  {
    private SyncDomainManager domainManager;

    protected Domain LocalDomain { get { return domainManager.LocalDomain; } }
    protected Domain RemoteDomain { get { return domainManager.RemoteDomain; } }

    [SetUp]
    public virtual void TestSetUp()
    {
    }

    [TearDown]
    public virtual void TestTearDown()
    {
    }

    [TestFixtureSetUp]
    public virtual void TestFixtureSetUp()
    {
      domainManager = new SyncDomainManager();
    }

    [TestFixtureTearDown]
    public virtual void TestFixtureTearDown()
    {
      if (domainManager!=null) {
        try {
          domainManager.Dispose();
        }
        finally {
          domainManager = null;
        }
      }
    }
  }
}