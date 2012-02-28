using System;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Xtensive.Disposing;

namespace Xtensive.Orm.BulkOperations.Tests
{
  public static class AssertEx
  {
    public static IDisposable ThatCommandsCount(IResolveConstraint expression)
    {
      Session session = Session.Current;
      int count = 0;
      session.Events.DbCommandExecuting += (sender, args) => count++;

      return new Disposable(a => Assert.That(count, expression));
    }
  }
}