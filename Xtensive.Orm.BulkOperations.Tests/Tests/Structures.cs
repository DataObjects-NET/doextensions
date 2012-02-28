using NUnit.Framework;
using Xtensive.Orm.Reprocessing;
using Xtensive.Orm.Reprocessing.Tests;
using Xtensive.Orm.Reprocessing.Tests.Model;

namespace Xtensive.Orm.BulkOperations.Tests.Tests
{
  internal class Structures : AutoBuildTest
  {
    [Test]
    public void StructuresSet()
    {
      Domain.Execute(
        session => {
          var bar = new Bar(session) {Count = 5};

          session.Query.All<Bar>().Set(a => a.Rectangle, new Rectangle {BorderWidth = 1}).Update();
          Assert.That(bar.Rectangle.BorderWidth, Is.EqualTo(1));

          session.Query.All<Bar>().Set(a => a.Rectangle, new Rectangle()).Update();
          Assert.That(bar.Rectangle.BorderWidth, Is.Null);

          session.Query.All<Bar>().Set(
            a => a.Rectangle, new Rectangle {BorderWidth = 2, First = new Point {X = 3, Y = 4}}).Update(
            );
          Assert.That(bar.Rectangle.BorderWidth, Is.EqualTo(2));
          Assert.That(bar.Rectangle.First.X, Is.EqualTo(3));
          Assert.That(bar.Rectangle.First.Y, Is.EqualTo(4));
          Assert.That(bar.Rectangle.Second.X, Is.Null);
          bar.Rectangle = new Rectangle();

          session.Query.All<Bar>().Set(a => a.Rectangle.First.X, 1).Update();
          Assert.That(bar.Rectangle.First.X, Is.EqualTo(1));
          Assert.That(bar.Rectangle.Second.X, Is.Null);
          bar.Rectangle = new Rectangle();

          /*var bar2 = new Bar(session);
            session.SaveChanges();
            using (AssertEx.ThatCommandsCount(Is.EqualTo(1)))
            {
                session.Query.All<Bar>().Where(a => a.Id == bar2.Id).Set(
                    a => a.Rectangle,
                    a=>session.Query.All<Bar>().Where(b => a.Id == bar.Id).Select(b => b.Rectangle).First()).
                    Update();
                Assert.That( counter.Count, Is.EqualTo(1));
            }
            Assert.That( bar2.Rectangle.First.X, Is.EqualTo(1));
            Assert.That(bar2.Rectangle.Second.X, Is.Null);
            bar2.Remove();*/

          session.Query.All<Bar>().Set(a => a.Rectangle.BorderWidth, a => a.Count * 2).Update();
          Assert.That(bar.Rectangle.BorderWidth, Is.EqualTo(10));

          bar.Rectangle = new Rectangle {First = new Point {X = 1, Y = 2}, Second = new Point {X = 3, Y = 4}};
          session.Query.All<Bar>().Set(a => a.Rectangle.BorderWidth, 1).Set(
            a => a.Rectangle.First, a => new Point {X = 2}).Update();
          Assert.That(
            bar.Rectangle,
            Is.EqualTo(
              new Rectangle {
                BorderWidth = 1,
                First = new Point {X = 2, Y = 2},
                Second = new Point {X = 3, Y = 4}
              }));
          bar.Rectangle = new Rectangle();

          bar.Rectangle = new Rectangle {First = new Point {X = 1, Y = 2}, Second = new Point {X = 3, Y = 4}};
          session.Query.All<Bar>().Set(a => a.Rectangle.BorderWidth, 1).Set(
            a => a.Rectangle.First, new Point {X = 2}).Update();
          Assert.That(
            bar.Rectangle,
            Is.EqualTo(
              new Rectangle {
                BorderWidth = 1,
                First = new Point {X = 2, Y = null},
                Second = new Point {X = 3, Y = 4}
              }));
          bar.Rectangle = new Rectangle();
        });
    }

    [Test]
    public void StructuresUpdate()
    {
      Domain.Execute(
        session => {
          var bar = new Bar(session) {Count = 5};

          session.Query.All<Bar>().Update(
            a => new Bar(null) {Rectangle = new Rectangle {BorderWidth = 1}});
          Assert.That(bar.Rectangle.BorderWidth, Is.EqualTo(1));

          session.Query.All<Bar>().Update(a => new Bar(null) {Rectangle = new Rectangle()});
          Assert.That(bar.Rectangle.BorderWidth, Is.Null);

          session.Query.All<Bar>().Update(
            a =>
              new Bar(null) {Rectangle = new Rectangle {BorderWidth = 2, First = new Point {X = 3, Y = 4}}});
          Assert.That(bar.Rectangle.BorderWidth, Is.EqualTo(2));
          Assert.That(bar.Rectangle.First.X, Is.EqualTo(3));
          Assert.That(bar.Rectangle.First.Y, Is.EqualTo(4));
          Assert.That(bar.Rectangle.Second.X, Is.Null);
          bar.Rectangle = new Rectangle();

          session.Query.All<Bar>().Update(
            a => new Bar(null) {Rectangle = new Rectangle {BorderWidth = a.Count * 2}});
          Assert.That(bar.Rectangle.BorderWidth, Is.EqualTo(10));
          bar.Rectangle = new Rectangle();

          bar.Rectangle = new Rectangle {First = new Point {X = 1, Y = 2}, Second = new Point {X = 3, Y = 4}};
          session.Query.All<Bar>().Update(
            a => new Bar(null) {Rectangle = new Rectangle {BorderWidth = 1, First = new Point {X = 2}}});
          Assert.That(
            bar.Rectangle,
            Is.EqualTo(
              new Rectangle {
                BorderWidth = 1,
                First = new Point {X = 2, Y = 2},
                Second = new Point {X = 3, Y = 4}
              }));
          bar.Rectangle = new Rectangle();

          var rectangle = new Rectangle {BorderWidth = 1, First = new Point {X = 2}};
          bar.Rectangle = new Rectangle {First = new Point {X = 1, Y = 2}, Second = new Point {X = 3, Y = 4}};
          session.Query.All<Bar>().Update(a => new Bar(null) {Rectangle = rectangle});
          Assert.That(bar.Rectangle, Is.EqualTo(rectangle));
          bar.Rectangle = new Rectangle();
        });
    }
  }
}