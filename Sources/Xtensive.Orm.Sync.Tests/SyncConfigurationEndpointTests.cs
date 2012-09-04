using System;
using System.Reflection;
using NUnit.Framework;
using Xtensive.Orm.Sync.Tests.Model;

namespace Xtensive.Orm.Sync.Tests
{
  [TestFixture]
  public class SyncConfigurationTests
  {
    [Test]
    public void IncludeAllTest()
    {
      var configuration = GetConfiguration();
      var sync = GetEndpoint(configuration);
      sync.All();

      Assert.IsTrue(configuration.SyncAll);
    }

    [Test]
    public void IncludeTypeTest()
    {
      var configuration = GetConfiguration();
      var sync = GetEndpoint(configuration);
      sync.All<MyEntity>();

      Assert.IsFalse(configuration.SyncAll);
      Assert.IsTrue(configuration.SyncTypes.Contains(typeof(MyEntity)));
    }

    [Test]
    public void IncludeAllAndIncludeTypeTest()
    {
      var configuration = GetConfiguration();
      var sync = GetEndpoint(configuration);
      sync
        .All()
        .All<MyEntity>();

      Assert.IsTrue(configuration.SyncAll);
      Assert.IsFalse(configuration.SyncTypes.Contains(typeof(MyEntity)));
    }

    [Test]
    public void IncludeAllAndSkipTypeTest()
    {
      var configuration = GetConfiguration();
      var sync = GetEndpoint(configuration);
      sync
        .All()
        .Skip<MyEntity>();

      Assert.IsTrue(configuration.SyncAll);
      Assert.IsFalse(configuration.SyncTypes.Contains(typeof(MyEntity)));
      Assert.IsTrue(configuration.SkipTypes.Contains(typeof(MyEntity)));
    }

    [Test]
    public void IncludeTypeAndSkipTypeTest()
    {
      var configuration = GetConfiguration();
      var sync = GetEndpoint(configuration);
      sync
        .All<MyEntity>()
        .Skip<MyEntity>();

      Assert.IsFalse(configuration.SyncTypes.Contains(typeof(MyEntity)));
      Assert.IsTrue(configuration.SkipTypes.Contains(typeof(MyEntity)));
    }

    [Test]
    public void FilterTypeTest()
    {
      var configuration = GetConfiguration();
      var sync = GetEndpoint(configuration);
      sync
        .All<MyEntity>(e => !e.IsRemoved);

      Assert.IsTrue(configuration.SyncTypes.Contains(typeof(MyEntity)));
      Assert.IsTrue(configuration.Filters.ContainsKey(typeof(MyEntity)));
      Assert.IsFalse(configuration.SkipTypes.Contains(typeof(MyEntity)));
    }

    [Test]
    public void FilterTypeAndSkipTypeTest()
    {
      var configuration = GetConfiguration();
      var sync = GetEndpoint(configuration);
      sync
        .All<MyEntity>(e => !e.IsRemoved)
        .Skip<MyEntity>();

      Assert.IsFalse(configuration.SyncTypes.Contains(typeof(MyEntity)));
      Assert.IsFalse(configuration.Filters.ContainsKey(typeof(MyEntity)));
      Assert.IsTrue(configuration.SkipTypes.Contains(typeof(MyEntity)));
    }

    [Test]
    public void ThrowOnNonHierarchyRootTest()
    {
      var configuration = GetConfiguration();
      var sync = GetEndpoint(configuration);
      Assert.Throws<InvalidOperationException>(
        () => sync
          .All<Entity>());
      Assert.Throws<InvalidOperationException>(
        () => sync
          .All()
          .All<Entity>());
      Assert.Throws<InvalidOperationException>(
        () => sync
          .All()
          .Skip<Entity>());
      Assert.Throws<InvalidOperationException>(
        () => sync
          .All()
          .All<Entity>(e => !e.IsRemoved));
    }

    [Test]
    public void ThrowOnDoubleFilterForTypeTest()
    {
      var configuration = GetConfiguration();
      var sync = GetEndpoint(configuration);
      Assert.Throws<InvalidOperationException>(
        () => sync
          .All<MyEntity>(e => !e.IsRemoved)
          .All<MyEntity>(e => !e.IsRemoved));
    }

    private SyncConfiguration GetConfiguration()
    {
      return (SyncConfiguration) Activator.CreateInstance(typeof (SyncConfiguration), true);
    }

    private SyncConfigurationEndpoint GetEndpoint(SyncConfiguration configuration)
    {
      return (SyncConfigurationEndpoint) Activator.CreateInstance(
        typeof (SyncConfigurationEndpoint),
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance,
        null, new object[] {configuration}, null);
    }
  }
}
