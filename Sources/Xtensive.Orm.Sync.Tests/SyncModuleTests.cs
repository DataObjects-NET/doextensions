﻿using System.Linq;
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

          var r = session.Services.Get<SyncMetadataStore>();
          Assert.IsNotNull(r);

          Assert.IsNotNull(r.ReplicaId);

          var m = LocalDomain.Services.Get<IDomainTrackingMonitor>();
          Assert.IsNotNull(m);

          var sp = LocalDomain.Services.Get<SyncProvider>();
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
  }
}