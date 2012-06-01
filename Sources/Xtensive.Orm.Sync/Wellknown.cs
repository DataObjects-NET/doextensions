using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  public static class Wellknown
  {
    public const uint SyncBatchSize = 256;
    public const string TickGeneratorName = "SyncInfo";

    public static SyncIdFormatGroup IdFormats { get; private set; }

    public static class FieldNames
    {
      public const string ReplicaId = "Xtensive.Orm.Sync.ReplicaId";
      public const string ReplicaTickCount = "Xtensive.Orm.Sync.ReplicaTickCount";
      public const string CurrentKnowledge = "Xtensive.Orm.Sync.CurrentKnowledge";
      public const string ForgottenKnowledge = "Xtensive.Orm.Sync.ForgottenKnowledge";
    }

    static Wellknown()
    {
      IdFormats = new SyncIdFormatGroup();
      IdFormats.ItemIdFormat.IsVariableLength = false;
      IdFormats.ItemIdFormat.Length = 16;
      IdFormats.ReplicaIdFormat.IsVariableLength = false;
      IdFormats.ReplicaIdFormat.Length = 16;
    }
  }
}
