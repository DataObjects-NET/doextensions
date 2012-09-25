using System;
using System.Linq;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  internal static class SyncIdFormatter
  {
    public static readonly SyncId MinId = GetUniformId(0x00);
    public static readonly SyncId MaxId = GetUniformId(0xFF);

    public static SyncId GetSyncId(SyncId replicaId, int hierarchyId, long tick)
    {
      var replicaIdBytes = replicaId.RawId;

      var buffer = new byte[16];

      buffer[0] = (byte) (replicaIdBytes[0] ^ replicaIdBytes[4] ^ replicaIdBytes[8] ^ replicaIdBytes[12]);
      buffer[1] = (byte) (replicaIdBytes[1] ^ replicaIdBytes[5] ^ replicaIdBytes[9] ^ replicaIdBytes[13]);
      buffer[2] = (byte) (replicaIdBytes[2] ^ replicaIdBytes[6] ^ replicaIdBytes[10] ^ replicaIdBytes[14]);
      buffer[3] = (byte) (replicaIdBytes[3] ^ replicaIdBytes[7] ^ replicaIdBytes[11] ^ replicaIdBytes[15]);

      ByteFormatter.WriteInt(buffer, 4, hierarchyId);
      ByteFormatter.WriteLong(buffer, 8, tick);

      return new SyncId(buffer, false);
    }

    public static SyncId GetSyncId(int replicaIdHash, int hierarchyId, long tick)
    {
      var buffer = new byte[16];
      ByteFormatter.WriteInt(buffer, 0, replicaIdHash);
      ByteFormatter.WriteInt(buffer, 4, hierarchyId);
      ByteFormatter.WriteLong(buffer, 8, tick);
      return new SyncId(buffer, false);
    }

    public static SyncId GetSyncId(string syncId)
    {
      var bytes = ByteFormatter.Parse(syncId);
      if (bytes.Length!=16)
        throw new InvalidOperationException(string.Format("Invalid sync id length: {0}", bytes.Length));
      return new SyncId(bytes, false);
    }

    public static SyncId GetLowerBound(SyncIdInfo info)
    {
      return GetSyncId(info.ReplicaIdHash, info.HierarchyId, 0);
    }

    public static SyncId GetUpperBound(SyncIdInfo info)
    {
      return GetSyncId(info.ReplicaIdHash, info.HierarchyId, -1);
    }

    public static SyncIdInfo GetInfo(SyncId syncId)
    {
      var buffer = syncId.RawId;
      return new SyncIdInfo(
        ByteFormatter.ReadInt(buffer, 0),
        ByteFormatter.ReadInt(buffer, 4),
        ByteFormatter.ReadLong(buffer, 8));
    }

    private static SyncId GetUniformId(byte value)
    {
      return new SyncId(Enumerable.Repeat(value, 16).ToArray(), false);
    }
  }
}
