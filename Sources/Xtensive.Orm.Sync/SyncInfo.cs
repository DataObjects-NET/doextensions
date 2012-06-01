using System;
using Microsoft.Synchronization;
using Xtensive.Orm.Model;
using Xtensive.Tuples;

namespace Xtensive.Orm.Sync
{
  [HierarchyRoot]
  [KeyGenerator(Name = Wellknown.TickGeneratorName)]
  [Index("GlobalId", Unique = true)]
  public abstract class SyncInfo : Entity, ISyncInfo
  {
    private SyncId cachedSyncId;
    private SyncVersion cachedCreatedVersion;
    private SyncVersion cachedChangeVersion;
    private SyncVersion cachedTombstoneVersion;

    /// <summary>
    /// Gets the id.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// Gets the global ID of the item.
    /// </summary>
    public Guid GlobalId { get; set; }

    /// <summary>
    /// Gets the global ID of the item.
    /// </summary>
    /// <value> The global ID of the item. </value>
    public SyncId SyncId
    {
      get
      {
        if (cachedSyncId!=null)
          return cachedSyncId;

        cachedSyncId = new SyncId(GlobalId);
        return cachedSyncId;
      }
    }

    /// <summary>
    /// Gets or sets a value that indicates whether the item has been deleted from the item store.
    /// </summary>
    /// 
    /// <returns>
    /// true if the item has been deleted; otherwise, false.
    /// </returns>
    public bool IsTombstone { get; set; }

    [Field]
    internal uint CreatedReplicaKey { get; set; }

    [Field]
    internal long CreatedTickCount { get; set; }

    /// <summary>
    /// Gets or sets the creation version for the item.
    /// </summary>
    /// <value> The creation version for the item. </value>
    /// <exception cref="T:System.ArgumentNullException">An attempt was made to set the value to a null.</exception>
    public SyncVersion CreatedVersion
    {
      get
      {
        if (cachedCreatedVersion!=null)
          return cachedCreatedVersion;

        cachedCreatedVersion = new SyncVersion(CreatedReplicaKey, (ulong) CreatedTickCount);
        return cachedCreatedVersion;
      }
      set
      {
        if (value==null)
          throw new ArgumentNullException("value");

        if (cachedCreatedVersion==value)
          return;

        CreatedReplicaKey = value.ReplicaKey;
        CreatedTickCount = (long) value.TickCount;

        cachedCreatedVersion = value;
      }
    }

    [Field]
    internal uint ChangeReplicaKey { get; set; }

    [Field]
    internal long ChangeTickCount { get; set; }

    /// <summary>
    /// Gets or sets the version of the most recent change made to the item.
    /// </summary>
    /// 
    /// <returns>
    /// The version of the most recent change made to the item. Returns a null when the change version has not been set.
    /// </returns>
    /// <exception cref="T:System.ArgumentNullException">An attempt was made to set the value to a null.</exception>
    public SyncVersion ChangeVersion
    {
      get
      {
        if (cachedChangeVersion!=null)
          return cachedChangeVersion;

        cachedChangeVersion = new SyncVersion(ChangeReplicaKey, (ulong) ChangeTickCount);
        return cachedChangeVersion;
      }
      set
      {
        if (value==null)
          throw new ArgumentNullException("value");

        if (cachedChangeVersion==value)
          return;

        ChangeReplicaKey = value.ReplicaKey;
        ChangeTickCount = (long) value.TickCount;

        cachedChangeVersion = value;
      }
    }

    [Field]
    internal uint TombstoneReplicaKey { get; set; }

    [Field]
    internal long TombstoneTickCount { get; set; }

    /// <summary>
    /// Gets or sets the version when the item was deleted.
    /// </summary>
    /// <value> The version when the item was deleted. </value>
    /// <exception cref="T:System.ArgumentNullException">An attempt was made to set the value to a null.</exception>
    public SyncVersion TombstoneVersion
    {
      get
      {
        if (cachedTombstoneVersion!=null)
          return cachedTombstoneVersion;

        cachedTombstoneVersion = new SyncVersion(TombstoneReplicaKey, (ulong) TombstoneTickCount);
        return cachedTombstoneVersion;
      }
      set
      {
        if (value==null)
          throw new ArgumentNullException("value");

        if (cachedTombstoneVersion==value)
          return;

        TombstoneReplicaKey = value.ReplicaKey;
        TombstoneTickCount = (long) value.TickCount;

        cachedTombstoneVersion = value;
      }
    }

    public string Text { get; set; }

    protected SyncInfo(Session session)
      : base(session)
    {
      GlobalId = Guid.NewGuid();
    }
  }
}