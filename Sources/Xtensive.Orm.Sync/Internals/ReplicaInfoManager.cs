using System;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Synchronization;
using Xtensive.IoC;
using Xtensive.Orm.Metadata;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (ReplicaInfoManager), Singleton = true)]
  internal sealed class ReplicaInfoManager : ISessionService
  {
    private static readonly XmlSerializer KnowledgeSerializer = new XmlSerializer(typeof (SyncKnowledge));
    private static readonly XmlSerializer ForgottenKnowledgeSerializer = new XmlSerializer(typeof (ForgottenKnowledge));

    private readonly Session session;
    private readonly SyncTickGenerator tickGenerator;

    public ReplicaInfo LoadReplicaInfo()
    {
      var result = new ReplicaInfo {Id = LoadReplicaId()};
      LoadCurrentKnowledge(result);
      LoadForgottenKnowledge(result);
      return result;
    }

    public void SaveReplicaInfo(ReplicaInfo info)
    {
      SaveCurrentKnowledge(info);
      SaveForgottenKnowledge(info);
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

    private void LoadCurrentKnowledge(ReplicaInfo info)
    {
      var container = GetContainer(WellKnown.CurrentKnowledgeExtensionName);
      if (container!=null) {
        var knowledge = (SyncKnowledge) Deserialize(KnowledgeSerializer, container.Text);
        info.CurrentKnowledge = knowledge;
        knowledge.SetLocalTickCount(GetLastTick());
      }
      else
        info.CurrentKnowledge = new SyncKnowledge(WellKnown.IdFormats, info.Id, GetLastTick());
    }

    private void LoadForgottenKnowledge(ReplicaInfo info)
    {
      var container = GetContainer(WellKnown.ForgottenKnowledgeExtensionName);
      if (container!=null)
        info.ForgottenKnowledge = (ForgottenKnowledge) Deserialize(ForgottenKnowledgeSerializer, container.Text);
      else
        info.ForgottenKnowledge = new ForgottenKnowledge(WellKnown.IdFormats, info.CurrentKnowledge);
    }

    private void SaveCurrentKnowledge(ReplicaInfo info)
    {
      var container = GetOrCreateContainer(WellKnown.CurrentKnowledgeExtensionName);
      container.Text = Serialize(KnowledgeSerializer, info.CurrentKnowledge);
    }

    private void SaveForgottenKnowledge(ReplicaInfo info)
    {
      var container = GetOrCreateContainer(WellKnown.ForgottenKnowledgeExtensionName);
      container.Text = Serialize(ForgottenKnowledgeSerializer, info.ForgottenKnowledge);
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
      return (ulong) tickGenerator.GetLastTick();
    }

    [ServiceConstructor]
    public ReplicaInfoManager(Session session, SyncTickGenerator tickGenerator)
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