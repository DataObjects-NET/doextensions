using Microsoft.Synchronization;
using Xtensive.Orm.Configuration;

namespace Xtensive.Orm.Sync
{
  internal static class WellKnown
  {
    public const int SyncBatchSize = 64;
    public const int EntityFetchBatchSize = 25;
    public const int SyncLogBatchSize = 512;
    public const int LocalReplicaKey = 0;
    public const int SyncInfoCacheSize = 128 * 1024;

    public const string TickGeneratorName = "SyncInfo";
    public const string EntityFieldName = "Entity";

    public const string ReplicaIdExtensionName = "Xtensive.Orm.Sync.ReplicaId";
    public const string CurrentKnowledgeExtensionName = "Xtensive.Orm.Sync.CurrentKnowledge";
    public const string ForgottenKnowledgeExtensionName = "Xtensive.Orm.Sync.ForgottenKnowledge";

    public static SyncIdFormatGroup IdFormats { get; private set; }

    public static SessionConfiguration SyncSessionConfiguration { get; private set; }

    static WellKnown()
    {
      IdFormats = new SyncIdFormatGroup();
      IdFormats.ItemIdFormat.IsVariableLength = false;
      IdFormats.ItemIdFormat.Length = 16;
      IdFormats.ReplicaIdFormat.IsVariableLength = false;
      IdFormats.ReplicaIdFormat.Length = 16;

      SyncSessionConfiguration = new SessionConfiguration("Sync", SessionOptions.ServerProfile);
      SyncSessionConfiguration.Lock();
    }
  }
}
