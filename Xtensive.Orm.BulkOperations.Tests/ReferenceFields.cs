using System;
using System.Linq;
using NUnit.Framework;
using Xtensive.Orm.Reprocessing;
using Xtensive.Orm.Reprocessing.Tests;
using Xtensive.Orm.Reprocessing.Tests.Model;

namespace Xtensive.Orm.BulkOperations.Tests
{
  internal class ReferenceFields : AutoBuildTest
  {
    [Test]
    public void ClientSideReferenceField()
    {
      using (var session = Domain.OpenSession()) {
        using (var trx = session.OpenTransaction()) {
          var bar = new Bar(session);
          var foo = new Foo(session);
          var bar2 = new Bar(session);
          session.Query.All<Foo>().Set(a => a.Bar, bar).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));
          session.Query.All<Foo>().Set(a => a.Bar, (Bar) null).Update();
          Assert.That(foo.Bar, Is.Null);
          session.Query.All<Foo>().Update(a => new Foo(session) {Bar = bar});
          Assert.That(bar, Is.EqualTo(foo.Bar));
          session.Query.All<Foo>().Update(a => new Foo(session) {Bar = null});
          Assert.That(foo.Bar, Is.Null);
          trx.Complete();
        }
      }
    }

    [Test]
    public void ClientSideReferenceField2()
    {
      using (var session = Domain.OpenSession()) {
        using (var trx = session.OpenTransaction()) {
          var bar = new Bar2(session, DateTime.Now, Guid.NewGuid());
          var foo = new Foo2(session, 1, "1");
          var bar2 = new Bar2(session, DateTime.Now, Guid.NewGuid());
          session.Query.All<Foo2>().Set(a => a.Bar, bar).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));
          session.Query.All<Foo2>().Set(a => a.Bar, (Bar2) null).Update();
          Assert.That(foo.Bar, Is.Null);
          session.Query.All<Foo2>().Update(a => new Foo2(null, 0, null) {Bar = bar});
          Assert.That(bar, Is.EqualTo(foo.Bar));
          session.Query.All<Foo2>().Update(a => new Foo2(null, 0, null) {Bar = null});
          Assert.That(foo.Bar, Is.Null);
          trx.Complete();
        }
      }
    }

    [Test]
    public void ServerSideReferenceField()
    {
      using (var session = Domain.OpenSession()) {
        using (var trx = session.OpenTransaction()) {
          var bar = new Bar(session);
          var foo = new Foo(session);
          var bara = new Bar(session);
          session.SaveChanges();
          Assert.That(foo.Bar, Is.Null);
          int one = 1;

          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo>().Set(
              a => a.Bar, a => session.Query.Single(Key.Create(Domain, typeof (Bar), 1))).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo>().Set(a => a.Bar, (Bar) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo>().Set(
              a => a.Bar, a => session.Query.Single<Bar>(Key.Create<Bar>(Domain, 1))).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo>().Set(a => a.Bar, (Bar) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo>().Set(a => a.Bar, a => session.Query.Single<Bar>(1)).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo>().Set(a => a.Bar, (Bar) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1))) {
            session.Query.All<Foo>().Set(
              a => a.Bar, a => session.Query.SingleOrDefault(Key.Create(Domain, typeof (Bar), 1))).
              Update();
          }
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo>().Set(a => a.Bar, (Bar) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1))) {
            session.Query.All<Foo>().Set(
              a => a.Bar, a => session.Query.SingleOrDefault<Bar>(Key.Create<Bar>(Domain, 1))).Update(
              );
          }
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo>().Set(a => a.Bar, (Bar) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo>().Set(a => a.Bar, a => session.Query.SingleOrDefault<Bar>(1)).Update(
              );
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo>().Set(a => a.Bar, (Bar) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo>().Set(
              a => a.Bar, a => session.Query.All<Bar>().Where(b => b.Id==one).First()).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo>().Set(a => a.Bar, (Bar) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo>().Set(
              a => a.Bar, a => session.Query.All<Bar>().First(b => b.Id==1)).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo>().Set(a => a.Bar, (Bar) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1))) {
            session.Query.All<Foo>().Set(
              a => a.Bar, a => session.Query.All<Bar>().Where(b => b.Id==1).FirstOrDefault()).Update
              ();
          }
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo>().Set(a => a.Bar, (Bar) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo>().Set(
              a => a.Bar, a => session.Query.All<Bar>().FirstOrDefault(b => b.Id==1)).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo>().Set(a => a.Bar, (Bar) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo>().Set(
              a => a.Bar, a => session.Query.All<Bar>().Where(b => b.Id==1).Single()).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo>().Set(a => a.Bar, (Bar) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo>().Set(
              a => a.Bar, a => session.Query.All<Bar>().Single(b => b.Id==1)).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo>().Set(a => a.Bar, (Bar) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1))) {
            session.Query.All<Foo>().Set(
              a => a.Bar, a => session.Query.All<Bar>().Where(b => b.Id==1).SingleOrDefault()).
              Update();
          }
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo>().Set(a => a.Bar, (Bar) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo>().Set(
              a => a.Bar, a => session.Query.All<Bar>().SingleOrDefault(b => b.Id==1)).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));
          trx.Complete();
        }
      }
    }

    [Test]
    public void ServerSideReferenceField2()
    {
      using (var session = Domain.OpenSession()) {
        using (var trx = session.OpenTransaction()) {
          DateTime date = DateTime.Now;
          Guid id = Guid.NewGuid();
          var bar = new Bar2(session, date, id);
          var foo = new Foo2(session, 1, "1");
          var bar2 = new Bar2(session, DateTime.Now, Guid.NewGuid());
          session.SaveChanges();
          Assert.That(foo.Bar, Is.Null);

          using (session.AssertCommandCount(Is.EqualTo(1))) {
            session.Query.All<Foo2>().Set(
              a => a.Bar, a => session.Query.Single(Key.Create(Domain, typeof (Bar2), date, id))).
              Update();
          }
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo2>().Set(a => a.Bar, (Bar2) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1))) {
            session.Query.All<Foo2>().Set(
              a => a.Bar, a => session.Query.Single<Bar2>(Key.Create<Bar2>(Domain, date, id))).Update(
              );
          }
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo2>().Set(a => a.Bar, (Bar2) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo2>().Set(a => a.Bar, a => session.Query.Single<Bar2>(date, id)).Update(
              );
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo2>().Set(a => a.Bar, (Bar2) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1))) {
            session.Query.All<Foo2>().Set(
              a => a.Bar,
              a => session.Query.SingleOrDefault(Key.Create(Domain, typeof (Bar2), date, id))).Update(
              );
          }
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo2>().Set(a => a.Bar, (Bar2) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1))) {
            session.Query.All<Foo2>().Set(
              a => a.Bar, a => session.Query.SingleOrDefault<Bar2>(Key.Create<Bar2>(Domain, date, id)))
              .Update();
          }
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo2>().Set(a => a.Bar, (Bar2) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo2>().Set(
              a => a.Bar, a => session.Query.SingleOrDefault<Bar2>(date, id)).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo2>().Set(a => a.Bar, (Bar2) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo2>().Set(
              a => a.Bar, a => session.Query.All<Bar2>().Where(b => b.Id2==id).First()).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo2>().Set(a => a.Bar, (Bar2) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo2>().Set(
              a => a.Bar, a => session.Query.All<Bar2>().First(b => b.Id2==id)).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo2>().Set(a => a.Bar, (Bar2) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1))) {
            session.Query.All<Foo2>().Set(
              a => a.Bar, a => session.Query.All<Bar2>().Where(b => b.Id2==id).FirstOrDefault()).
              Update();
          }
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo2>().Set(a => a.Bar, (Bar2) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo2>().Set(
              a => a.Bar, a => session.Query.All<Bar2>().FirstOrDefault(b => b.Id2==id)).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo2>().Set(a => a.Bar, (Bar2) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo2>().Set(
              a => a.Bar, a => session.Query.All<Bar2>().Where(b => b.Id2==id).Single()).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo2>().Set(a => a.Bar, (Bar2) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo2>().Set(
              a => a.Bar, a => session.Query.All<Bar2>().Single(b => b.Id2==id)).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo2>().Set(a => a.Bar, (Bar2) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1))) {
            session.Query.All<Foo2>().Set(
              a => a.Bar, a => session.Query.All<Bar2>().Where(b => b.Id2==id).SingleOrDefault()).
              Update();
          }
          Assert.That(bar, Is.EqualTo(foo.Bar));

          session.Query.All<Foo2>().Set(a => a.Bar, (Bar2) null).Update();
          Assert.That(foo.Bar, Is.Null);
          using (session.AssertCommandCount(Is.EqualTo(1)))
            session.Query.All<Foo2>().Set(
              a => a.Bar, a => session.Query.All<Bar2>().SingleOrDefault(b => b.Id2==id)).Update();
          Assert.That(bar, Is.EqualTo(foo.Bar));
          trx.Complete();
        }
      }
    }
  }
}