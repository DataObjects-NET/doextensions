using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Synchronization;
using NUnit.Framework;
using Xtensive.Orm.Sync.Tests.Model;

namespace Xtensive.Orm.Sync.Tests
{
  [TestFixture]
  public class FirstTests : AutoBuildTest
  {
    [Test]
    public void FirstTest()
    {
      using (var session = LocalDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          new MyEntity(session);
          t.Complete();
        }
      }

      using (var session = RemoteDomain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          new MyReferenceProperty(session);
          t.Complete();
        }
      }

      var orchestrator = new SyncOrchestrator {
          LocalProvider = LocalDomain.Services.Get<SyncProvider>(),
          RemoteProvider = RemoteDomain.Services.Get<SyncProvider>(),
          Direction = SyncDirectionOrder.UploadAndDownload
        };
      orchestrator.Synchronize();
    }
  }
}
