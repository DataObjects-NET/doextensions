using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Synchronization;
using NUnit.Framework;
using Xtensive.Orm.Sync.Tests.Model;

namespace Xtensive.Orm.Sync.Tests
{
  [TestFixture]
  public class SerializationTests : AutoBuildTest
  {
    [Test]
    public void SerializeIdentityTest()
    {
      var key = Key.Create<MyEntity>(LocalDomain, 1L);
      var id1 = new Identity(Guid.NewGuid(), key);
      Identity id2;

      var formatter = new BinaryFormatter();
      using (var ms = new MemoryStream()) {
        formatter.Serialize(ms, id1);
        ms.Position = 0;
        id2 = (Identity) formatter.Deserialize(ms);
        id2.BindTo(RemoteDomain);
      }
      id1.CompareTo(id2);
    }

    [Test]
    public void SerializeItemChangeDataTest()
    {
      var key1 = Key.Create<MyEntity>(LocalDomain, 1L);
      var id1 = new Identity(Guid.NewGuid(), key1);
      var key2 = Key.Create<MyEntity>(LocalDomain, 2L);
      var id2 = new Identity(Guid.NewGuid(), key2);
      var data1 = new ItemChangeData();
      data1.Identity = id1;
      data1.Tuple = Tuples.Tuple.Create(1, "string");
      data1.References.Add("field1", id2);
      ItemChangeData data2;

      var formatter = new BinaryFormatter();
      using (var ms = new MemoryStream()) {
        formatter.Serialize(ms, data1);
        ms.Position = 0;
        data2 = (ItemChangeData) formatter.Deserialize(ms);
        data2.BindTo(RemoteDomain);
      }
      data1.CompareTo(data2);
    }

    [Test]
    public void SerializeChangeSetTest()
    {
      var key1 = Key.Create<MyEntity>(LocalDomain, 1L);
      var id1 = new Identity(Guid.NewGuid(), key1);
      var key2 = Key.Create<MyEntity>(LocalDomain, 2L);
      var id2 = new Identity(Guid.NewGuid(), key2);
      var data = new ItemChangeData();
      data.Identity = id1;
      data.Tuple = Tuples.Tuple.Create(1, "string");
      data.References.Add("field1", id2);
      var changeSet1 = new ChangeSet();
      changeSet1.Add(data);
      ChangeSet changeSet2;
      var formatter = new BinaryFormatter();
      using (var ms = new MemoryStream()) {
        formatter.Serialize(ms, changeSet1);
        ms.Position = 0;
        changeSet2 = (ChangeSet) formatter.Deserialize(ms);
        changeSet2.BindTo(RemoteDomain);
      }
      changeSet1.CompareTo(changeSet2);
    }

    [Test]
    public void SerializeChangeDataRetrieverTest()
    {
      var key1 = Key.Create<MyEntity>(LocalDomain, 1L);
      var id1 = new Identity(Guid.NewGuid(), key1);
      var key2 = Key.Create<MyEntity>(LocalDomain, 2L);
      var id2 = new Identity(Guid.NewGuid(), key2);
      var data = new ItemChangeData();
      data.Identity = id1;
      data.Tuple = Tuples.Tuple.Create(1, "string");
      data.References.Add("field1", id2);
      var changeSet = new ChangeSet();
      changeSet.Add(data);

      var idFormats = new SyncIdFormatGroup();
      idFormats.ItemIdFormat.IsVariableLength = false;
      idFormats.ItemIdFormat.Length = 16;
      idFormats.ReplicaIdFormat.IsVariableLength = false;
      idFormats.ReplicaIdFormat.Length = 16;

      var dataRetriever1 = new ChangeDataRetriever(idFormats, changeSet);
      ChangeDataRetriever dataRetriever2;

      var formatter = new BinaryFormatter();
      using (var ms = new MemoryStream()) {
        formatter.Serialize(ms, dataRetriever1);
        ms.Position = 0;
        dataRetriever2 = (ChangeDataRetriever) formatter.Deserialize(ms);
        dataRetriever2.BindTo(RemoteDomain);
      }
    }
  }

  public static class Extensions
  {
    public static void CompareTo(this Identity left, Identity right)
    {
      Assert.AreEqual(left.GlobalId, right.GlobalId);
      Assert.AreEqual(left.Key.Value, right.Key.Value);
    }

    public static void CompareTo(this ItemChangeData left, ItemChangeData right)
    {
      left.Identity.CompareTo(right.Identity);
      Assert.AreEqual(left.Tuple, right.Tuple);
      var r1 = left.References.First();
      var r2 = right.References.First();
      Assert.AreEqual(r1.Key, r2.Key);
      r1.Value.CompareTo(r2.Value);
    }

    public static void CompareTo(this ChangeSet left, ChangeSet right)
    {
      left.First().CompareTo(right.First());
    }
  }
}
