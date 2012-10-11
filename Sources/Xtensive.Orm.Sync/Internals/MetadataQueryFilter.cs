namespace Xtensive.Orm.Sync
{
  internal sealed class MetadataQueryFilter
  {
    public uint ReplicaKey { get; private set; }

    public long? LastKnownTick { get; private set; }

    public override string ToString()
    {
      return LastKnownTick!=null
        ? string.Format("replica = {0} and tick > {1}", ReplicaKey, LastKnownTick)
        : string.Format("replica = {0}", ReplicaKey);
    }

    public MetadataQueryFilter(uint replicaKey, long? lastKnownTick = null)
    {
      ReplicaKey = replicaKey;
      LastKnownTick = lastKnownTick;
    }
  }
}