using System.Linq;
using System.Reflection;
using Microsoft.Synchronization;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Sync.Model;

namespace Xtensive.Orm.Sync
{
  internal static class WellKnown
  {
    /// <summary>
    /// Default value for <see cref="SyncConfiguration.BatchSize"/>.
    /// </summary>
    public const int SyncBatchSize = 64;

    /// <summary>
    /// Number of objects to fetch at once when fetching
    /// ordered metadata.
    /// </summary>
    public const int OrderedMetadataBatchSize = 1024;

    /// <summary>
    /// Number of objects to fetch at once when fetching unordered metadata
    /// </summary>
    public const int UnorderedMetadataBatchSize = 25;

    /// <summary>
    /// Number of object to read when applying <see cref="SyncLog"/>.
    /// </summary>
    public const int SyncLogBatchSize = 512;

    /// <summary>
    /// Number of metadata items to cache on destination side.
    /// </summary>
    public const int SyncInfoCacheSize = 128 * 1024;

    public const uint LocalReplicaKey = 0;

    public const string TickGeneratorName = "SyncInfo";
    public const string EntityFieldName = "Entity";

    public const string ReplicaIdExtensionName = "Xtensive.Orm.Sync.ReplicaId";
    public const string CurrentKnowledgeExtensionName = "Xtensive.Orm.Sync.CurrentKnowledge";
    public const string ForgottenKnowledgeExtensionName = "Xtensive.Orm.Sync.ForgottenKnowledge";

    public static SyncIdFormatGroup IdFormats { get; private set; }

    public static SessionConfiguration SyncSessionConfiguration { get; private set; }

    public static MethodInfo StringCompareToMethod { get; private set; }

    static WellKnown()
    {
      IdFormats = new SyncIdFormatGroup();
      IdFormats.ItemIdFormat.IsVariableLength = false;
      IdFormats.ItemIdFormat.Length = 16;
      IdFormats.ReplicaIdFormat.IsVariableLength = false;
      IdFormats.ReplicaIdFormat.Length = 16;

      SyncSessionConfiguration = new SessionConfiguration("Sync", SessionOptions.ServerProfile);
      SyncSessionConfiguration.Lock();

      StringCompareToMethod =
        typeof (string).GetMethods()
          .Select(m => new {Method = m, Parameters = m.GetParameters()})
          .Where(info => info.Method.Name=="CompareTo" && info.Parameters.Length==1 && info.Parameters[0].ParameterType==typeof (string))
          .Select(info => info.Method)
          .Single();
    }
  }
}
