using System.Text;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  internal sealed class MetadataQuery
  {
    public SyncId MinId { get; private set; }

    public SyncId MaxId { get; private set; }

    public uint? ReplicaKey { get; private set; }

    public long? LastKnownTick { get; private set; }

    public override string ToString()
    {
      var result = new StringBuilder();
      if (MinId!=null && MaxId!=null)
        result.AppendFormat("[{0}, {1}) ", MinId, MaxId);
      if (ReplicaKey!=null)
        result.AppendFormat("replica = {0} ", ReplicaKey.Value);
      if (LastKnownTick!=null)
        result.AppendFormat("tick > {0}", LastKnownTick.Value);
      return result.ToString();
    }

    public MetadataQuery(SyncId minId = null, SyncId maxId = null, uint? replicaKey = null, long? lastKnownTick = null)
    {
      MinId = minId;
      MaxId = maxId;
      ReplicaKey = replicaKey;
      LastKnownTick = lastKnownTick;
    }
  }
}