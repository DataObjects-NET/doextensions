using System;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  internal static class SyncIdBuilder
  {
    public static SyncId GetSyncId(int globalTypeId, SyncId replicaId, long tick)
    {
      var replicaIdBytes = replicaId.RawId;

      var buffer = new byte[16];

      buffer[0] = (byte) ((globalTypeId >> 24) & 255);
      buffer[1] = (byte) ((globalTypeId >> 16) & 255);
      buffer[2] = (byte) ((globalTypeId >> 8) & 255);
      buffer[3] = (byte) (globalTypeId & 255);

      buffer[4] = (byte) (replicaIdBytes[0] ^ replicaIdBytes[4] ^ replicaIdBytes[8] ^ replicaIdBytes[12]);
      buffer[5] = (byte) (replicaIdBytes[1] ^ replicaIdBytes[5] ^ replicaIdBytes[9] ^ replicaIdBytes[13]);
      buffer[6] = (byte) (replicaIdBytes[2] ^ replicaIdBytes[6] ^ replicaIdBytes[10] ^ replicaIdBytes[14]);
      buffer[7] = (byte) (replicaIdBytes[3] ^ replicaIdBytes[7] ^ replicaIdBytes[11] ^ replicaIdBytes[15]);

      buffer[8] = (byte) ((tick >> 56) & 255);
      buffer[9] = (byte) ((tick >> 48) & 255);
      buffer[10] = (byte) ((tick >> 40) & 255);
      buffer[11] = (byte) ((tick >> 32) & 255);
      buffer[12] = (byte) ((tick >> 24) & 255);
      buffer[13] = (byte) ((tick >> 16) & 255);
      buffer[14] = (byte) ((tick >> 8) & 255);
      buffer[15] = (byte) (tick & 255);

      return new SyncId(buffer, false);
    }

    public static SyncId GetSyncId(string syncId)
    {
      var buffer = new byte[16];

      for (int i = 0; i < 16; i++) {
        var high = ParseDigit(syncId[2 * i]);
        var low = ParseDigit(syncId[2 * i + 1]);
        buffer[i] = (byte) (((high & 255) << 4) | (low & 255));
      }

      return new SyncId(buffer, false);
    }

    private static int ParseDigit(char ch)
    {
      if (ch >= '0' && ch <= '9')
        return ch - '0';

      if (ch >= 'a' && ch <= 'f')
        return ch - 'a';

      if (ch >= 'A' && ch <= 'F')
        return ch - 'A';

      throw new InvalidOperationException(string.Format("'{0}' is not correct hex digit", ch));
    }
  }
}
