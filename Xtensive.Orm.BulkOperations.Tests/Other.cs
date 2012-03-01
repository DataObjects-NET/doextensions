using System;
using System.Linq;
using NUnit.Framework;
using Xtensive.Orm.Reprocessing.Tests;
using Xtensive.Orm.Reprocessing.Tests.Model;

namespace Xtensive.Orm.BulkOperations.Tests
{
  internal class Other : AutoBuildTest
  {
    [Test]
    public void CompositeKeyUpdate()
    {
      using (var session = Domain.OpenSession()) {
        using (var trx = session.OpenTransaction()) {
          DateTime date1 = DateTime.Now;
          DateTime date2 = DateTime.Now.AddDays(1);
          Guid id1 = Guid.NewGuid();
          Guid id2 = Guid.NewGuid();
          var foo1 = new Bar2(session, date1, id1) {Name = "test"};
          var foo2 = new Bar2(session, date2, id1);
          var foo3 = new Bar2(session, date2, id2) {Name = "test"};
          int updated = session.Query.All<Bar2>().Where(a => a.Name=="test").Set(a => a.Name, "abccba").Update();
          Assert.That(updated, Is.EqualTo(2));
          Assert.That(foo1.Name, Is.EqualTo("abccba"));
          Assert.That(foo3.Name, Is.EqualTo("abccba"));
          Assert.That(foo2.Name, Is.Null);
          trx.Complete();
        }
      }
    }

    [Test]
    public void SimpleDelete()
    {
      using (var session = Domain.OpenSession()) {
        using (var trx = session.OpenTransaction()) {
          var bar1 = new Bar(session) {Name = "test", Count = 3};
          var bar2 = new Bar(session);
          var bar3 = new Bar(session);
          bar3.Foo.Add(new Foo(session) {Name = "Foo"});
          string s = "test";

          int deleted = session.Query.All<Bar>().Where(a => a.Name==s).Delete();
          Assert.That(bar1.IsRemoved, Is.True);
          Assert.That(bar2.IsRemoved, Is.False);
          Assert.That(bar3.IsRemoved, Is.False);
          Assert.That(deleted, Is.EqualTo(1));

          session.Query.All<Bar>().Where(a => a.Foo.Any(b => b.Name=="Foo")).Update(a => new Bar(null) {Name = ""});
          deleted = session.Query.All<Bar>().Where(a => a.Foo.Count(b => b.Name=="Foo")==0).Delete();
          Assert.That(bar2.IsRemoved, Is.True);
          Assert.That(bar3.IsRemoved, Is.False);
          Assert.That(deleted, Is.EqualTo(1));
          trx.Complete();
        }
      }
    }

    [Test]
    public void SimpleUpdate()
    {
      using (var session = Domain.OpenSession()) {
        using (var trx = session.OpenTransaction()) {
          var bar1 = new Bar(session) {Name = "test", Count = 3};
          var bar2 = new Bar(session);
          string s = "test";
          string s1 = "abccba";
          int updated =
            session.Query.All<Bar>().Where(a => a.Name.Contains(s)).Update(
              a => new Bar(session) {Name = a.Name + s1, Count = a.Count * 2, Description = null});
          Assert.That(bar1.Name, Is.EqualTo("testabccba"));
          Assert.That(bar1.Description, Is.Null);
          Assert.That(bar1.Count, Is.EqualTo(6));
          Assert.That(updated, Is.EqualTo(1));
          trx.Complete();
        }
      }
    }

    [Test]
    public void SubqueryUpdate()
    {
      using (var session = Domain.OpenSession()) {
        using (var trx = session.OpenTransaction()) {
          var bar = new Bar(session);
          var bar2 = new Bar(session) {Count = 1};
          new Foo(session) {Bar = bar, Name = "test"};
          new Foo(session) {Bar = bar, Name = "test1"};
          session.Query.All<Bar>().Where(a => a.Count==a.Foo.Count - 2).Set(a => a.Count, a => a.Foo.Count).Update();
          Assert.That(bar.Count, Is.EqualTo(2));
          session.AssertCommandCount(
            Is.EqualTo(1),
            () =>
              session.Query.All<Bar>().Where(a => a.Count==session.Query.All<Bar>().Max(b => b.Count)).Set(
                a => a.Count, a => session.Query.All<Bar>().Min(b => b.Count)).Update());
          Assert.That(bar.Count, Is.EqualTo(1));
          trx.Complete();
        }
      }
    }

    [Test]
    public void UpdateViaSet()
    {
      using (var session = Domain.OpenSession()) {
        using (var trx = session.OpenTransaction()) {
          var bar1 = new Bar(session) {Name = "test", Count = 3};
          var bar2 = new Bar(session);
          string s = "test";
          string s1 = "abccba";
          int updated =
            session.Query.All<Bar>().Where(a => a.Name.Contains(s)).Set(a => a.Name, s1).Set(
              a => a.Count, a => a.Count * 2).Set(a => a.Description, a => a.Name + s1).Update();
          Assert.That(bar1.Name, Is.EqualTo("abccba"));
          Assert.That(bar1.Description, Is.EqualTo("testabccba"));
          Assert.That(bar1.Count, Is.EqualTo(6));
          Assert.That(updated, Is.EqualTo(1));
          trx.Complete();
        }
      }
    }

    [Test]
    [ExpectedException(typeof (NotSupportedException))]
    public void UpdateWithReferenceToUpdatingEntity()
    {
      using (var session = Domain.OpenSession()) {
        using (var trx = session.OpenTransaction()) {
          var foo1 = new Foo(session) {Name = "Test"};
          var foo2 = new Foo(session);
          var foo3 = new Foo(session) {Name = "Test1"};
          var bar1 = new Bar(session) {Name = "Test"};
          var bar2 = new Bar(session);
          session.Query.All<Foo>().Set(a => a.Bar, a => session.Query.All<Bar>().First(b => b.Name==a.Name)).Update();
          Assert.That(foo1.Bar, Is.EqualTo(bar1));
          Assert.That(foo2.Bar, Is.EqualTo(bar2));
          Assert.That(foo3.Bar, Is.Null);
          trx.Complete();
        }
      }
    }
  }
}