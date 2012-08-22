using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  internal static class Wellknown
  {
    public const uint SyncBatchSize = 64;
    public const int KeyPreloadBatchSize = 25;
    public const int LocalReplicaKey = 0;
    public const string TickGeneratorName = "SyncInfo";
    public const string EntityFieldName = "Entity";
    public const string ReplicaIdFieldName = "Xtensive.Orm.Sync.ReplicaId";
    public const string CurrentKnowledgeFieldName = "Xtensive.Orm.Sync.CurrentKnowledge";
    public const string ForgottenKnowledgeFieldName = "Xtensive.Orm.Sync.ForgottenKnowledge";

    public static SyncIdFormatGroup IdFormats { get; private set; }

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
