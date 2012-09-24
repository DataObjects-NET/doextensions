namespace Xtensive.Orm.Sync
{
  internal struct SyncIdInfo
  {
    public readonly int ReplicaIdHash;
    public readonly int HierarchyId;
    public readonly long Tick;

    public SyncIdInfo(int replicaIdHash, int hiearchyId, long tick)
    {
      ReplicaIdHash = replicaIdHash;
      HierarchyId = hiearchyId;
      Tick = tick;
    }
  }
}