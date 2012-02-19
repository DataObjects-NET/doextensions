using NUnit.Framework;
using Xtensive.Orm.Reprocessing.Tests.Model;

namespace Xtensive.Orm.Reprocessing.Tests
{
  public class Other : AutoBuildTest
  {
    [Test]
    public void NestedSessionReuse()
    {
      Domain.Execute(session1 => Domain.Execute(session2 => Assert.That(session1, Is.SameAs(session2))));
    }

    [Test]
    public void NestedNewSession()
    {
      Domain.Execute(
          session =>
          {
            using (Session.Deactivate())
            {
              Domain.Execute(session2 => Assert.That(session2, Is.Not.SameAs(session)));
            }
          });
    }

    [Test]
    public void ExecuteStrategy()
    {
      int i = 0;
      try
      {
        var config = Domain.GetReprocessingConfiguration();
        config.DefaultExecuteStrategy = typeof(TestExecuteStrategy);
        Domain.Execute(
            session =>
            {
              new Foo(session) { Name = "test" };
              i++;
              if (i < 5)
                new Foo(session) { Name = "test" };
            });
      }
      catch
      {
        Assert.That(i, Is.EqualTo(2));
      }
    }

    #region Nested type: TestExecuteStrategy

    private class TestExecuteStrategy : HandleUniqueConstraintViolationStrategy
    {
      protected override bool OnError(ExecuteErrorEventArgs context)
      {
        return context.Attempt < 2;
      }
    }

    #endregion
  }
}