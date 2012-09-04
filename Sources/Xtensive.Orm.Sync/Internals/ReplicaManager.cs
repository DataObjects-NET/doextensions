using System;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Synchronization;
using Xtensive.IoC;
using Xtensive.Orm.Metadata;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (ReplicaManager), Singleton = true)]
  internal sealed class ReplicaManager : ISessionService
  {
    private static readonly XmlSerializer KnowledgeSerializer = new XmlSerializer(typeof (SyncKnowledge));
    private static readonly XmlSerializer ForgottenKnowledgeSerializer = new XmlSerializer(typeof (ForgottenKnowledge));

    private readonly Session session;
    private readonly SyncTickGenerator tickGenerator;

    public Replica LoadReplica()
    {
      var result = new Replica {Id = LoadReplicaId()};
      LoadCurrentKnowledge(result);
      LoadForgottenKnowledge(result);
      return result;
    }

    public void SaveReplica(Replica replica)
    {
      SaveCurrentKnowledge(replica);
      SaveForgottenKnowledge(replica);
    }

    public SyncId LoadReplicaId()
    {
      var container = GetContainer(WellKnown.ReplicaIdExtensionName);
      if (container!=null)
        return new SyncId(new Guid(container.Text));
      var syncId = Guid.NewGuid();
      CreateContainer(WellKnown.ReplicaIdExtensionName, syncId.ToString());
      return new SyncId(syncId);
    }

    private void LoadCurrentKnowledge(Replica replica)
    {
      var container = GetContainer(WellKnown.CurrentKnowledgeExtensionName);
      if (container!=null) {
        var knowledge = (SyncKnowledge) Deserialize(KnowledgeSerializer, container.Text);
        replica.CurrentKnowledge = knowledge;
        knowledge.SetLocalTickCount(GetLastTick());
      }
      else
        replica.CurrentKnowledge = new SyncKnowledge(WellKnown.IdFormats, replica.Id, GetLastTick());
    }

    private void LoadForgottenKnowledge(Replica replica)
    {
      var container = GetContainer(WellKnown.ForgottenKnowledgeExtensionName);
      if (container!=null)
        replica.ForgottenKnowledge = (ForgottenKnowledge) Deserialize(ForgottenKnowledgeSerializer, container.Text);
      else
        replica.ForgottenKnowledge = new ForgottenKnowledge(WellKnown.IdFormats, replica.CurrentKnowledge);
    }

    private void SaveCurrentKnowledge(Replica replica)
    {
      var container = GetOrCreateContainer(WellKnown.CurrentKnowledgeExtensionName);
      container.Text = Serialize(KnowledgeSerializer, replica.CurrentKnowledge);
    }

    private void SaveForgottenKnowledge(Replica replica)
    {
      var container = GetOrCreateContainer(WellKnown.ForgottenKnowledgeExtensionName);
      container.Text = Serialize(ForgottenKnowledgeSerializer, replica.ForgottenKnowledge);
    }

    private Extension GetContainer(string name)
    {
      return session.Query.SingleOrDefault<Extension>(name);
    }

    private void CreateContainer(string name, string text)
    {
      using (session.Activate())
        new Extension(name) {Text = text};
    }

    private Extension GetOrCreateContainer(string name)
    {
      var result = session.Query.SingleOrDefault<Extension>(name);
      if (result==null)
        using (session.Activate())
          result = new Extension(name);
      return result;
    }

    private string Serialize(XmlSerializer serializer, object data)
    {
      if (data==null)
        return null;

      var writer = new StringWriter();
      serializer.Serialize(writer, data);
      return writer.ToString();
    }

    private object Deserialize(XmlSerializer serializer, string data)
    {
      if (string.IsNullOrEmpty(data))
        return null;

      return serializer.Deserialize(new StringReader(data));
    }

    private ulong GetLastTick()
    {
      return (ulong) tickGenerator.GetLastTick(session);
    }

    [ServiceConstructor]
    public ReplicaManager(Session session, SyncTickGenerator tickGenerator)
    {
      if (session==null)
        throw new ArgumentNullException("session");
      if (tickGenerator==null)
        throw new ArgumentNullException("tickGenerator");

      this.session = session;
      this.tickGenerator = tickGenerator;
    }
  }
}