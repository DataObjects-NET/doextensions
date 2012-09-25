using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  internal sealed class ReplicaInfo
  {
    public SyncId Id { get; set; }

    public SyncKnowledge CurrentKnowledge { get; set; }

    public ForgottenKnowledge ForgottenKnowledge { get; set; }
  }
}
