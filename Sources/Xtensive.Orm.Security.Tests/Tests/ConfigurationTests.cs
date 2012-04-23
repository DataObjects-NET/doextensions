// Copyright (C) 2011 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2011.06.07

using System;
using System.Configuration;
using NUnit.Framework;
using Xtensive.Orm.Security.Configuration;

namespace Xtensive.Orm.Security.Tests
{
  [TestFixture]
  public class ConfigurationTests
  {
    [Test]
    public void HashingServiceNameTest()
    {
      var section = (Configuration.ConfigurationSection) ConfigurationManager.GetSection("Xtensive.Orm.Security.WithName");
      Assert.IsNotNull(section);
      Assert.IsNotNull(section.HashingService);
      Assert.IsNotNull(section.HashingService.Name);
      Assert.AreEqual("md5", section.HashingService.Name);

      var config = SecurityConfiguration.Load("Xtensive.Orm.Security.WithName");
      Assert.IsNotNull(config);
      Assert.AreEqual("md5", config.HashingServiceName);
    }

    [Test]
    public void HashingServiceEmptyTest()
    {
      var section = (Configuration.ConfigurationSection) ConfigurationManager.GetSection("Xtensive.Orm.Security.WithoutName");
      Assert.IsNotNull(section);
      Assert.IsNotNull(section.HashingService);
      Assert.IsNullOrEmpty(section.HashingService.Name);

      var config = SecurityConfiguration.Load("Xtensive.Orm.Security.WithoutName");
      Assert.IsNotNull(config);
      Assert.AreEqual("plain", config.HashingServiceName);
    }

    [Test]
    public void HashingServiceAbsentTest()
    {
      var section = (Configuration.ConfigurationSection) ConfigurationManager.GetSection("Xtensive.Orm.Security.Empty");
      Assert.IsNotNull(section);
      Assert.IsNotNull(section.HashingService);
      Assert.IsNullOrEmpty(section.HashingService.Name);

      var config = SecurityConfiguration.Load("Xtensive.Orm.Security.Empty");
      Assert.IsNotNull(config);
      Assert.AreEqual("plain", config.HashingServiceName);
    }

    [Test]
    public void HashingServiceNoConfigTest()
    {
      var section = (Configuration.ConfigurationSection) ConfigurationManager.GetSection("Xtensive.Orm.Security.XXX");
      Assert.IsNull(section);

      var config = SecurityConfiguration.Load("Xtensive.Orm.Security.XXX");
      Assert.IsNotNull(config);
      Assert.AreEqual("plain", config.HashingServiceName);
    }

    [Test]
    public void HashingServiceDefaultTest()
    {
      var section = (Configuration.ConfigurationSection) ConfigurationManager.GetSection("Xtensive.Orm.Security");
      Assert.IsNotNull(section);
      Assert.IsNotNull(section.HashingService);
      Assert.IsNotNullOrEmpty(section.HashingService.Name);
      Assert.AreEqual("sha1", section.HashingService.Name);

      var config = SecurityConfiguration.Load();
      Assert.IsNotNull(config);
      Assert.AreEqual("sha1", config.HashingServiceName);
    }
  }
}