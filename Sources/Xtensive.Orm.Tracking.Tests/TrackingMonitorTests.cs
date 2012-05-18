// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2012.05.17

using System.Linq;
using NUnit.Framework;
using Xtensive.Orm.Tracking.Tests.Model;

namespace Xtensive.Orm.Tracking.Tests
{
  [TestFixture]
  public class TrackingMonitorTests : AutoBuildTest
  {
    private bool listenerIsCalled;

    public override void TestSetUp()
    {
      listenerIsCalled = false;
    }

    private void CheckListenerIsCalled()
    {
      listenerIsCalled = true;
    }

    [Test]
    public void CreateInOutermostTest()
    {
        using (var session = Domain.OpenSession()) {

          session.StartTracking(e => {
            CheckListenerIsCalled();
            Assert.IsNotNull(e.Items);
            Assert.AreEqual(1, e.Items.Count());
            var ti = e.Items.First();
            Assert.AreEqual(TrackingItemState.Created, ti.State);
            Assert.IsNotNull(ti.Key);
            Assert.IsNotNull(ti.RawData);
          });

          using (var t = session.OpenTransaction()) {
            new MyEntity(session);
            t.Complete();
          }
          session.StopTracking();
        }
        Assert.IsTrue(listenerIsCalled);
    }

    [Test]
    public void CreateAndModifyInOutermostTest()
    {
        using (var session = Domain.OpenSession()) {

          session.StartTracking(e1 => {
            CheckListenerIsCalled();
            Assert.IsNotNull(e1.Items);
            Assert.AreEqual(1, e1.Items.Count());
            var ti = e1.Items.First();
            Assert.AreEqual(TrackingItemState.Created, ti.State);
            Assert.IsNotNull(ti.Key);
            Assert.IsNotNull(ti.RawData);
            Assert.AreEqual("some text", ti.RawData.GetValue(2));
          });

          using (var t = session.OpenTransaction()) {
            var e = new MyEntity(session);
            session.SaveChanges();
            e.Text = "some text";
            t.Complete();
          }
          session.StopTracking();
        }
        Assert.IsTrue(listenerIsCalled);
    }

    [Test]
    public void CreateAndRemoveInOutermostTest()
    {
        using (var session = Domain.OpenSession()) {

          session.StartTracking(e1 => {
            CheckListenerIsCalled();
            Assert.IsNotNull(e1.Items);
            Assert.AreEqual(1, e1.Items.Count());
            var ti = e1.Items.First();
            Assert.AreEqual(TrackingItemState.Removed, ti.State);
            Assert.IsNotNull(ti.Key);
            Assert.IsNotNull(ti.RawData);
            Assert.AreEqual("some text", ti.RawData.GetValue(2));
          });

          using (var t = session.OpenTransaction()) {
            var e = new MyEntity(session);
            e.Text = "some text";
            session.SaveChanges();
            e.Remove();
            t.Complete();
          }
          session.StopTracking();
        }
        Assert.IsTrue(listenerIsCalled);
    }

    [Test]
    public void CreateAndRollbackInOutermostTest()
    {
        using (var session = Domain.OpenSession()) {

          session.StartTracking(e1 => {
            throw new AssertionException("This must not be called when outermost transaction is rolled back");
          });

          using (var t = session.OpenTransaction()) {
            var e = new MyEntity(session);
            //t.Complete();  Emulating transaction rollback
          }
          session.StopTracking();
        }
    }

    [Test]
    public void CreateInOutermostAndNestedTest()
    {
        using (var session = Domain.OpenSession()) {

          session.StartTracking(e => {
            CheckListenerIsCalled();
            Assert.IsNotNull(e.Items);
            Assert.AreEqual(2, e.Items.Count());
            var ti = e.Items.First();
            Assert.AreEqual(TrackingItemState.Created, ti.State);
            Assert.IsNotNull(ti.Key);
            Assert.IsNotNull(ti.RawData);
            Assert.AreEqual("some text", ti.RawData.GetValue(2));
            ti = e.Items.Skip(1).First();
            Assert.AreEqual(TrackingItemState.Created, ti.State);
            Assert.IsNotNull(ti.Key);
            Assert.IsNotNull(ti.RawData);
            Assert.AreEqual("another text", ti.RawData.GetValue(2));
          });

          using (var t = session.OpenTransaction()) {
            var e1 = new MyEntity(session);
            e1.Text = "some text";
            using (var t2 = session.OpenTransaction(TransactionOpenMode.New)) {
              var e2 = new MyEntity(session);
              e2.Text = "another text";
              t2.Complete();
            }
            t.Complete();
          }
          session.StopTracking();
        }
        Assert.IsTrue(listenerIsCalled);
    }

    [Test]
    public void CreateInOutermostAndModifyInNestedTest()
    {
        using (var session = Domain.OpenSession()) {

          session.StartTracking(e1 => {
            CheckListenerIsCalled();
            Assert.IsNotNull(e1.Items);
            Assert.AreEqual(1, e1.Items.Count());
            var ti = e1.Items.First();
            Assert.AreEqual(TrackingItemState.Created, ti.State);
            Assert.IsNotNull(ti.Key);
            Assert.IsNotNull(ti.RawData);
            Assert.AreEqual("some text", ti.RawData.GetValue(2));
            Assert.AreEqual("another text", ti.RawData.GetValue(3));
          });

          using (var t = session.OpenTransaction()) {
            var e = new MyEntity(session);
            e.Text = "some text";
            session.SaveChanges();
            using (var t2 = session.OpenTransaction(TransactionOpenMode.New)) {
              e.Text2 = "another text";
              t2.Complete();
            }
            t.Complete();
          }
          session.StopTracking();
        }
        Assert.IsTrue(listenerIsCalled);
    }

    [Test]
    public void CreateInOutermostAndRemoveInNestedTest()
    {
        using (var session = Domain.OpenSession()) {

          session.StartTracking(e1 => {
            CheckListenerIsCalled();
            Assert.IsNotNull(e1.Items);
            Assert.AreEqual(1, e1.Items.Count());
            var ti = e1.Items.First();
            Assert.AreEqual(TrackingItemState.Removed, ti.State);
            Assert.IsNotNull(ti.Key);
            Assert.IsNotNull(ti.RawData);
            Assert.AreEqual("some text", ti.RawData.GetValue(2));
          });

          using (var t = session.OpenTransaction()) {
            var e = new MyEntity(session);
            e.Text = "some text";
            session.SaveChanges();
            using (var t2 = session.OpenTransaction(TransactionOpenMode.New)) {
              e.Remove();
              t2.Complete();
            }
            t.Complete();
          }
          session.StopTracking();
        }
        Assert.IsTrue(listenerIsCalled);
    }

    [Test]
    public void RemoveInOutermostAndCreateInNestedTest()
    {
        using (var session = Domain.OpenSession()) {

          session.StartTracking(e1 => {
            CheckListenerIsCalled();
            Assert.IsNotNull(e1.Items);
            Assert.AreEqual(1, e1.Items.Count());
            var ti = e1.Items.First();
            Assert.AreEqual(TrackingItemState.Modified, ti.State);
            Assert.IsNotNull(ti.Key);
            Assert.IsNotNull(ti.RawData);
            Assert.AreEqual(null, ti.RawData.GetValue(2));
            Assert.AreEqual("another text", ti.RawData.GetValue(3));
          });

          using (var t = session.OpenTransaction()) {
            var e = new MyEntity(session);
            int id = e.Id;
            e.Text = "some text";
            e.Remove();
            session.SaveChanges();
            using (var t2 = session.OpenTransaction(TransactionOpenMode.New)) {
              e = new MyEntity(session, id);
              e.Text2 = "another text";
              t2.Complete();
            }
            t.Complete();
          }
          session.StopTracking();
        }
        Assert.IsTrue(listenerIsCalled);
    }

    [Test]
    public void CreateAndModifyInNextTest()
    {

      Key key;
      using (var session = Domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          var e = new MyEntity(session);
          key = e.Key;
          e.Text = "some text";
          t.Complete();
        }
      }

        using (var session = Domain.OpenSession()) {

          session.StartTracking(e1 => {
            CheckListenerIsCalled();
            Assert.IsNotNull(e1.Items);
            Assert.AreEqual(1, e1.Items.Count());
            var ti = e1.Items.First();
            Assert.AreEqual(TrackingItemState.Modified, ti.State);
            Assert.IsNotNull(ti.Key);
            Assert.IsNotNull(ti.RawData);
            Assert.AreEqual(1, ti.GetChangedValues().Count());
            var changedValue = ti.GetChangedValues().First();
            Assert.AreEqual("some text", changedValue.OriginalValue);
            Assert.AreEqual("another text", changedValue.NewValue);
          });

          using (var t = session.OpenTransaction()) {
            var e = session.Query.Single<MyEntity>(key);
            e.Text = "another text";
            t.Complete();
          }
          session.StopTracking();
        }
        Assert.IsTrue(listenerIsCalled);
    }
  }
}