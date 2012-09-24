using System;
using System.Globalization;
using System.Text;

namespace Xtensive.Orm.Sync
{
  internal static class ByteFormatter
  {
    public static void WriteInt(byte[] buffer, int offset, int value)
    {
      buffer[offset] = (byte) ((value >> 24) & 255);
      buffer[offset + 1] = (byte) ((value >> 16) & 255);
      buffer[offset + 2] = (byte) ((value >> 8) & 255);
      buffer[offset + 3] = (byte) (value & 255);
    }

    public static void WriteLong(byte[] buffer, int offset, long value)
    {
      buffer[offset] = (byte) ((value >> 56) & 255);
      buffer[offset + 1] = (byte) ((value >> 48) & 255);
      buffer[offset + 2] = (byte) ((value >> 40) & 255);
      buffer[offset + 3] = (byte) ((value >> 32) & 255);
      buffer[offset + 4] = (byte) ((value >> 24) & 255);
      buffer[offset + 5] = (byte) ((value >> 16) & 255);
      buffer[offset + 6] = (byte) ((value >> 8) & 255);
      buffer[offset + 7] = (byte) (value & 255);
    }

    public static int ReadInt(byte[] buffer, int offset)
    {
      int item0 = buffer[offset];
      int item1 = buffer[offset + 1];
      int item2 = buffer[offset + 2];
      int item3 = buffer[offset + 3];

      return (item0 << 24)
        | (item1 << 16)
        | (item2 << 8)
        | item3;
    }

    public static int ReadLong(byte[] buffer, int offset)
    {
      int item0 = buffer[offset];
      int item1 = buffer[offset + 1];
      int item2 = buffer[offset + 2];
      int item3 = buffer[offset + 3];
      int item4 = buffer[offset + 4];
      int item5 = buffer[offset + 5];
      int item6 = buffer[offset + 6];
      int item7 = buffer[offset + 7];

      return (item0 << 56)
        | (item1 << 48)
        | (item2 << 40)
        | (item3 << 32)
        | (item4 << 24)
        | (item5 << 16)
        | (item6 << 8)
        | item7;
    }

    public static string Format(byte[] buffer)
    {
      var result = new StringBuilder();
      foreach (byte b in buffer)
        result.Append(b.ToString("x2", CultureInfo.InvariantCulture));
      return result.ToString();
    }

    public static byte[] Parse(string value)
    {
      var bytesCount = value.Length / 2;
      var buffer = new byte[bytesCount];

      for (int i = 0; i < bytesCount; i++) {
        var high = ParseDigit(value[2 * i]);
        var low = ParseDigit(value[2 * i + 1]);
        buffer[i] = (byte) (((high & 255) << 4) | (low & 255));
      }

      return buffer;
    }

    private static int ParseDigit(char ch)
    {
      if (ch >= '0' && ch <= '9')
        return ch - '0';

      if (ch >= 'a' && ch <= 'f')
        return ch - 'a' + 10;

      if (ch >= 'A' && ch <= 'F')
        return ch - 'A' + 10;

      throw new InvalidOperationException(string.Format("'{0}' is not correct hex digit", ch));
    }
  }
}