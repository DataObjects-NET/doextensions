using System;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Synchronization;
using Xtensive.Orm.Metadata;

namespace Xtensive.Orm.Sync
{
  internal class Replica : SessionBound
  {
    private static Lazy<XmlSerializer> knowledgeSerializer;
    private static Lazy<XmlSerializer> forgottenKnowledgeSerializer;

    private readonly SyncTickGenerator tickGenerator;
    private SyncKnowledge currentKnowledge;
    private ForgottenKnowledge forgottenKnowledge;

    public SyncId Id { get; private set; }

    public SyncKnowledge CurrentKnowledge
    {
      get { return currentKnowledge; }
      set
      {
        UpdateCurrentKnowledge(value);
        currentKnowledge = value;
      }
    }

    public ForgottenKnowledge ForgottenKnowledge
    {
      get { return forgottenKnowledge; }
      set
      {
        UpdateForgottenKnowledge(value);
        forgottenKnowledge = value;
      }
    }

    public long TickCount
    {
      get { return tickGenerator.GetLastTick(Session); }
    }

    public long NextTick
    {
      get { return tickGenerator.GetNextTick(Session); }
    }

    private void Initialize()
    {
      ReadId();
      ReadCurrentKnowledge();
      ReadForgottenKnowledge();
    }

    private void ReadId()
    {
      var container = Session.Query.SingleOrDefault<Extension>(Wellknown.ReplicaIdFieldName);
      if (container==null) {
        Id = new SyncId(Guid.NewGuid());
        using (Session.Activate())
          new Extension(Wellknown.ReplicaIdFieldName) {
            Text = Id.GetGuidId().ToString()
          };
      }
      else
        Id = new SyncId(new Guid(container.Text));
    }

    private void ReadCurrentKnowledge()
    {
      var container = Session.Query.SingleOrDefault<Extension>(Wellknown.CurrentKnowledgeFieldName);
      if (container!=null) {
        CurrentKnowledge = (SyncKnowledge) knowledgeSerializer.Value.Deserialize(new StringReader(container.Text));
        CurrentKnowledge.SetLocalTickCount((ulong) TickCount);
      }
      else
        CurrentKnowledge = new SyncKnowledge(Wellknown.IdFormats, Id, (ulong) TickCount);
    }

    private void ReadForgottenKnowledge()
    {
      var container = Session.Query.SingleOrDefault<Extension>(Wellknown.ForgottenKnowledgeFieldName);
      if (container!=null)
        ForgottenKnowledge = (ForgottenKnowledge) forgottenKnowledgeSerializer.Value.Deserialize(new StringReader(container.Text));
      else
        ForgottenKnowledge = new ForgottenKnowledge(Wellknown.IdFormats, CurrentKnowledge);
    }

    private void UpdateCurrentKnowledge(SyncKnowledge knowledge)
    {
      if (knowledge==null)
        throw new ArgumentNullException("knowledge");

      var container = Session.Query.SingleOrDefault<Extension>(Wellknown.CurrentKnowledgeFieldName);
      if (container==null)
        using (Session.Activate())
          container = new Extension(Wellknown.CurrentKnowledgeFieldName);
      var writer = new StringWriter();
      knowledgeSerializer.Value.Serialize(writer, knowledge);
      container.Text = writer.ToString();
    }

    private void UpdateForgottenKnowledge(ForgottenKnowledge knowledge)
    {
      if (knowledge==null)
        return;

      var container = Session.Query.SingleOrDefault<Extension>(Wellknown.ForgottenKnowledgeFieldName);
      if (container==null)
        using (Session.Activate())
          container = new Extension(Wellknown.ForgottenKnowledgeFieldName);

      var writer = new StringWriter();
      forgottenKnowledgeSerializer.Value.Serialize(writer, knowledge);
      container.Text = writer.ToString();
    }

    public Replica(Session session)
      : base(session)
    {
      tickGenerator = session.Domain.Services.Get<SyncTickGenerator>();
      knowledgeSerializer = new Lazy<XmlSerializer>(() => new XmlSerializer(typeof(SyncKnowledge)));
      forgottenKnowledgeSerializer = new Lazy<XmlSerializer>(() => new XmlSerializer(typeof(ForgottenKnowledge)));
      Initialize();
    }
  }
}
