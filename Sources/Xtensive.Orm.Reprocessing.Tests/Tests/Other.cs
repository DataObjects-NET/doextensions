﻿using System.Transactions;
using NUnit.Framework;
using Xtensive.Orm;
using Xtensive.Orm.Reprocessing;
using Xtensive.Orm.Reprocessing.Configuration;
using TestCommon.Model;
using System.Linq;
using Xtensive.Orm.Reprocessing.Tests;

namespace TestCommon.Model
{
  public class Other : AutoBuildTest
  {
    private class TestExecuteStrategy : HandleUniqueConstraintViolationStrategy
    {
      #region Non-public methods

      protected override bool OnError(ExecuteErrorEventArgs context)
      {
        return context.Attempt < 2;
      }

      #endregion
    }

    [Test]
    public void ExecuteStrategy()
    {
      int i = 0;
      try {
        ReprocessingConfiguration config = Domain.GetReprocessingConfiguration();
        config.DefaultExecuteStrategy = typeof (TestExecuteStrategy);
        Domain.Execute(
          session => {
            new Foo(session) {Name = "test"};
            i++;
            if (i < 5)
              new Foo(session) {Name = "test"};
          });
      }
      catch {
        Assert.That(i, Is.EqualTo(2));
      }
    }

    [Test]
    public void NestedNewSession()
    {
      Domain.Execute(
        session => {
          using (Session.Deactivate()) {
            Domain.Execute(session2 => Assert.That(session2, Is.Not.SameAs(session)));
          }
        });
    }

    [Test]
    public void NestedSessionReuse()
    {
      Domain.Execute(session1 => Domain.Execute(session2 => Assert.That(session1, Is.SameAs(session2))));
    }

    [Test]
    public void Test()
    {
      Domain.Execute(session => {
        new Foo(session);
      });
      Domain.Execute(session => {
        session.Query.All<Foo>().ToArray();
        Domain.WithIsolationLevel(IsolationLevel.Serializable).Execute(session2 => {
          session2.Query.All<Foo>().ToArray();
        });
      });
    }
  }
}