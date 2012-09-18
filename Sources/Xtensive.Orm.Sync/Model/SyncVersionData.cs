using System;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync.Model
{
  /// <summary>
  /// Persistent storage for <see cref="SyncVersion"/>.
  /// </summary>
  public sealed class SyncVersionData : Structure
  {
    private SyncVersion version;

    /// <summary>
    /// Gets replica for this version.
    /// </summary>
    [Field]
    public uint Replica { get; private set; }

    /// <summary>
    /// Gets tick for this version.
    /// </summary>
    [Field]
    public long Tick { get; private set; }

    /// <summary>
    /// Gets or sets <see cref="SyncVersion"/>.
    /// </summary>
    public SyncVersion Version
    {
      get
      {
        if (version!=null)
          return version;
        version = new SyncVersion(Replica, (ulong) Tick);
        return version;
      }
    }

    /// <summary>
    /// Creates new instance of <see cref="SyncVersionData"/> type.
    /// </summary>
    /// <param name="session">Session to use.</param>
    /// <param name="version">Version.</param>
    public SyncVersionData(Session session, SyncVersion version)
      : base(session)
    {
      if (version==null)
        throw new ArgumentNullException("version");
      Replica = version.ReplicaKey;
      Tick = (long) version.TickCount;
      this.version = version;
    }

    /// <summary>
    /// Creates new instance of <see cref="SyncVersionData"/> type.
    /// </summary>
    /// <param name="session">Session to use.</param>
    /// <param name="replicaKey">Replica key.</param>
    /// <param name="tick">Tick count.</param>
    public SyncVersionData(Session session, uint replicaKey, long tick)
      : base(session)
    {
      Replica = replicaKey;
      Tick = tick;
    }
  }
}