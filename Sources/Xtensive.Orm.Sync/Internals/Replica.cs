using System;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Synchronization;
using Xtensive.Orm.Metadata;

namespace Xtensive.Orm.Sync
{
  internal class Replica : SessionBound
  {
    private static readonly XmlSerializer KnowledgeSerializer = new XmlSerializer(typeof (SyncKnowledge));
    private static readonly XmlSerializer ForgottenKnowledgeSerializer = new XmlSerializer(typeof (ForgottenKnowledge));

    private readonly SyncTickGenerator tickGenerator;

    public SyncId Id { get; private set; }

    public SyncKnowledge CurrentKnowledge { get; private set; }

    public ForgottenKnowledge ForgottenKnowledge { get; private set; }

    public long GetTickCount()
    {
      return tickGenerator.GetLastTick(Session);
    }

    public long GetNextTick()
    {
      return tickGenerator.GetNextTick(Session);
    }

    public void UpdateState()
    {
      UpdateCurrentKnowledge(CurrentKnowledge);
      UpdateForgottenKnowledge(ForgottenKnowledge);
    }

    private void ReadId()
    {
      var container = Session.Query.SingleOrDefault<Extension>(WellKnown.ReplicaIdExtensionName);
      if (container!=null) {
        Id = new SyncId(new Guid(container.Text));
      }
      else {
        Id = new SyncId(Guid.NewGuid());
        using (Session.Activate())
          new Extension(WellKnown.ReplicaIdExtensionName) {Text = Id.GetGuidId().ToString()};
      }
    }

    private void ReadCurrentKnowledge()
    {
      var container = Session.Query.SingleOrDefault<Extension>(WellKnown.CurrentKnowledgeExtensionName);
      if (container!=null) {
        CurrentKnowledge = (SyncKnowledge) KnowledgeSerializer.Deserialize(new StringReader(container.Text));
        CurrentKnowledge.SetLocalTickCount((ulong) GetTickCount());
      }
      else
        CurrentKnowledge = new SyncKnowledge(WellKnown.IdFormats, Id, (ulong) GetTickCount());
    }

    private void ReadForgottenKnowledge()
    {
      var container = Session.Query.SingleOrDefault<Extension>(WellKnown.ForgottenKnowledgeExtensionName);
      if (container!=null)
        ForgottenKnowledge = (ForgottenKnowledge) ForgottenKnowledgeSerializer.Deserialize(new StringReader(container.Text));
      else
        ForgottenKnowledge = new ForgottenKnowledge(WellKnown.IdFormats, CurrentKnowledge);
    }

    private void UpdateCurrentKnowledge(SyncKnowledge knowledge)
    {
      if (knowledge==null)
        throw new ArgumentNullException("knowledge");

      var container = Session.Query.SingleOrDefault<Extension>(WellKnown.CurrentKnowledgeExtensionName);
      if (container==null)
        using (Session.Activate())
          container = new Extension(WellKnown.CurrentKnowledgeExtensionName);

      var writer = new StringWriter();
      KnowledgeSerializer.Serialize(writer, knowledge);
      container.Text = writer.ToString();
    }

    private void UpdateForgottenKnowledge(ForgottenKnowledge knowledge)
    {
      if (knowledge==null)
        return;

      var container = Session.Query.SingleOrDefault<Extension>(WellKnown.ForgottenKnowledgeExtensionName);
      if (container==null)
        using (Session.Activate())
          container = new Extension(WellKnown.ForgottenKnowledgeExtensionName);

      var writer = new StringWriter();
      ForgottenKnowledgeSerializer.Serialize(writer, knowledge);
      container.Text = writer.ToString();
    }

    public Replica(Session session)
      : base(session)
    {
      tickGenerator = session.Domain.Services.Get<SyncTickGenerator>();

      ReadId();
      ReadCurrentKnowledge();
      ReadForgottenKnowledge();
    }
  }
}
