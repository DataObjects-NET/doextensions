// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2012.05.16

using NUnit.Framework;

namespace Xtensive.Orm.Tracking.Tests
{
  [TestFixture]
  public class ServiceRegistrationTest : AutoBuildTest
  {
    [Test]
    public void ShouldReturnInstanceOfTrackingMonitor()
    {
      var tm = Domain.Services.Get<DomainTrackingMonitor>();
      Assert.IsNotNull(tm);
    }

    [Test]
    public void ShouldReturnSameInstanceOfTrackingMonitor()
    {
      var tm1 = Domain.Services.Get<DomainTrackingMonitor>();
      var tm2 = Domain.Services.Get<DomainTrackingMonitor>();
      Assert.AreSame(tm1, tm2);
    }
  }
}