using System;
using NUnit.Framework;
using Xtensive.Core;
using Xtensive.Orm.Configuration;
using Xtensive.Reflection;

namespace Xtensive.Orm.Sync.Tests
{
  [TestFixture]
  public abstract class AutoBuildTest
  {
    #region Setup/Teardown

    [SetUp]
    public virtual void TestSetUp()
    {
    }

    [TearDown]
    public virtual void TestTearDown()
    {
    }

    #endregion

    protected Domain LocalDomain { get; private set; }
    protected Domain RemoteDomain { get; private set; }

    [TestFixtureSetUp]
    public virtual void TestFixtureSetUp()
    {
      var config = BuildLocalConfiguration();
      LocalDomain = BuildDomain(config);
      config = BuildRemoteConfiguration();
      RemoteDomain = BuildDomain(config);
    }

    [TestFixtureTearDown]
    public virtual void TestFixtureTearDown()
    {
      LocalDomain.DisposeSafely();
      RemoteDomain.DisposeSafely();
    }

    protected virtual DomainConfiguration BuildRemoteConfiguration()
    {
      return DomainConfiguration.Load("remote");
    }

    protected virtual DomainConfiguration BuildLocalConfiguration()
    {
      return DomainConfiguration.Load("local");
    }

    protected virtual Domain BuildDomain(DomainConfiguration configuration)
    {
      try {
        return Domain.Build(configuration);
      }
      catch (Exception e) {
        Log.Error(GetType().GetFullName());
        Log.Error(e);
        throw;
      }
    }
  }
}