using NUnit.Framework;
using System;
using Xtensive.Core;
using Xtensive.Orm.Tracking.Tests.Model;
using Xtensive.Reflection;
using Xtensive.Orm.Configuration;

namespace Xtensive.Orm.Tracking.Tests
{
  [TestFixture]
  public abstract class AutoBuildTest
  {
    protected Domain Domain { get; private set; }

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
      var config = BuildConfiguration();
      Domain = BuildDomain(config);
    }

    [TestFixtureTearDown]
    public virtual void TestFixtureTearDown()
    {
      Domain.DisposeSafely();
    }

    protected virtual DomainConfiguration BuildConfiguration()
    {
      return DomainConfiguration.Load("default");
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
