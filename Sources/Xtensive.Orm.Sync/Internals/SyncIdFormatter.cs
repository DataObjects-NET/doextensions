using System;
using System.Linq;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  internal static class SyncIdFormatter
  {
    public static readonly SyncId MinId = GetUniformId(0x00);
    public static readonly SyncId MaxId = GetUniformId(0xFF);

    public static SyncId GetSyncId(int hierarchyId, SyncId replicaId, long tick)
    {
      var replicaIdBytes = replicaId.RawId;

      var buffer = new byte[16];

      ByteFormatter.WriteInt(buffer, 0, hierarchyId);

      buffer[4] = (byte) (replicaIdBytes[0] ^ replicaIdBytes[4] ^ replicaIdBytes[8] ^ replicaIdBytes[12]);
      buffer[5] = (byte) (replicaIdBytes[1] ^ replicaIdBytes[5] ^ replicaIdBytes[9] ^ replicaIdBytes[13]);
      buffer[6] = (byte) (replicaIdBytes[2] ^ replicaIdBytes[6] ^ replicaIdBytes[10] ^ replicaIdBytes[14]);
      buffer[7] = (byte) (replicaIdBytes[3] ^ replicaIdBytes[7] ^ replicaIdBytes[11] ^ replicaIdBytes[15]);

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

    public static SyncId GetLowerBound(int hierarchyId)
    {
      return GetSyncId(hierarchyId, 0, 0);
    }

    public static SyncId GetUpperBound(int hierarchyId)
    {
      return GetSyncId(hierarchyId, -1, -1);
    }

    public static SyncId GetLowerBound(SyncIdInfo info)
    {
      return GetSyncId(info.HierarchyId, info.ReplicaIdHash, 0);
    }

    public static SyncId GetUpperBound(SyncIdInfo info)
    {
      return GetSyncId(info.HierarchyId, info.ReplicaIdHash, -1);
    }

    public static SyncId GetNextId(SyncId syncId)
    {
      var info = GetInfo(syncId);
      return GetSyncId(info.HierarchyId, info.ReplicaIdHash, info.Tick + 1);
    }

    public static SyncIdInfo GetInfo(SyncId syncId)
    {
      var buffer = syncId.RawId;
      return new SyncIdInfo(
        ByteFormatter.ReadInt(buffer, 0),
        ByteFormatter.ReadInt(buffer, 4),
        ByteFormatter.ReadLong(buffer, 8));
    }

    private static SyncId GetSyncId(int hierarchyId, int replicaIdHash, long tick)
    {
      var buffer = new byte[16];
      ByteFormatter.WriteInt(buffer, 0, hierarchyId);
      ByteFormatter.WriteInt(buffer, 4, replicaIdHash);
      ByteFormatter.WriteLong(buffer, 8, tick);
      return new SyncId(buffer, false);
    }

    private static SyncId GetUniformId(byte value)
    {
      return new SyncId(Enumerable.Repeat(value, 16).ToArray(), false);
    }
  }
}
