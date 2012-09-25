namespace Xtensive.Orm.Sync
{
  internal struct SyncIdInfo
  {
    public readonly int HierarchyId;
    public readonly int ReplicaIdHash;
    public readonly long Tick;

    public SyncIdInfo(int hiearchyId, int replicaIdHash, long tick)
    {
      HierarchyId = hiearchyId;
      ReplicaIdHash = replicaIdHash;
      Tick = tick;
    }
  }
}