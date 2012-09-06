using NUnit.Framework;

namespace Xtensive.Orm.Sync.Tests
{
  [TestFixture]
  public abstract class MultisyncTest
  {
    private SyncDomainManager domainManager;

    protected Domain LocalDomain { get { return domainManager.LocalDomain; } }
    protected Domain RemoteDomain { get { return domainManager.RemoteDomain; } }

    [SetUp]
    public virtual void TestSetUp()
    {
      domainManager = new SyncDomainManager();
    }

    [TearDown]
    public virtual void TestTearDown()
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

    [TestFixtureSetUp]
    public virtual void TestFixtureSetUp()
    {
    }

    [TestFixtureTearDown]
    public virtual void TestFixtureTearDown()
    {
    }
  }
}