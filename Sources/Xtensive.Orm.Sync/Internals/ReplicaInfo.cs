using System.Collections.Generic;
using System.Linq;
using Microsoft.Synchronization;
using Xtensive.Core;

namespace Xtensive.Orm.Sync
{
  internal sealed class ReplicaInfo
  {
    public SyncId Id { get; set; }

    public SyncKnowledge CurrentKnowledge { get; set; }

    public ForgottenKnowledge ForgottenKnowledge { get; set; }

    public HashSet<uint> GetKnownReplicas()
    {
      return new KnowledgeFragmentInspector(CurrentKnowledge).ScopeRangeSet
        .SelectMany(range => range.ClockVector.Select(item => item.ReplicaKey))
        .Concat(Enumerable.Repeat(WellKnown.LocalReplicaKey, 1))
        .ToHashSet();
    }
  }
}
