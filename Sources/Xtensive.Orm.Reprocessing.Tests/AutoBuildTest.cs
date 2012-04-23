using System.Collections.Generic;
using NUnit.Framework;
using Xtensive.Core;
using Xtensive.Orm.Configuration;

namespace Xtensive.Orm.Reprocessing.Tests
{
  [TestFixture]
  public abstract class AutoBuildTest
  {
    private List<Session> notDisposed;
    protected Domain Domain { get; private set; }

    [SetUp]
    public virtual void SetUp()
    {
      DomainConfiguration config = BuildConfiguration();
      Domain = BuildDomain(config);
      notDisposed = new List<Session>();
      Domain.SessionOpen += (sender, args) => {
        notDisposed.Add(args.Session);
        args.Session.Events.Disposing += (o, eventArgs) => {
          lock (notDisposed) {
            notDisposed.Remove(args.Session);
          }
        };
      };
      PopulateData();
    }

    [TearDown]
    public virtual void TearDown()
    {
      Assert.That(notDisposed, Is.Empty);
      Assert.That(SessionScope.CurrentSession, Is.Null);
      Domain.DisposeSafely();
    }

    protected virtual DomainConfiguration BuildConfiguration()
    {
      return DomainConfiguration.Load("Default");
    }

    protected virtual Domain BuildDomain(DomainConfiguration configuration)
    {
      return Domain.Build(configuration);
    }

    protected virtual void PopulateData()
    {
    }
  }
}