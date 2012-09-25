using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  internal sealed class MetadataQuery
  {
    public string MinId { get; private set; }

    public string MaxId { get; private set; }

    public SyncVersion LastKnownVersion { get; private set; }

    public IList<uint> ReplicasToExclude { get; private set; }

    public override string ToString()
    {
      var result = new StringBuilder();
      if (!string.IsNullOrEmpty(MinId) && !string.IsNullOrEmpty(MaxId))
        result.AppendFormat(" [{0}, {1})", MinId, MaxId);
      if (LastKnownVersion!=null)
        result.AppendFormat(" replica = {0} and tick > {1}", LastKnownVersion.ReplicaKey, LastKnownVersion.TickCount);
      if (ReplicasToExclude!=null && ReplicasToExclude.Count > 0)
        result.AppendFormat(" replica not in ({0})", string.Join(", ", ReplicasToExclude.Select(r => r.ToString())));
      return result.ToString();
    }

    public MetadataQuery(string minId = null, string maxId = null, SyncVersion lastKnownVersion = null, IEnumerable<uint> replicasToExclude = null)
    {
      MinId = minId;
      MaxId = maxId;
      LastKnownVersion = lastKnownVersion;

      if (replicasToExclude!=null)
        ReplicasToExclude = replicasToExclude.ToList().AsReadOnly();
    }
  }
}