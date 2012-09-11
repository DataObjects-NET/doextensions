using System;
using Microsoft.Synchronization;
using Xtensive.Aspects;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// <see cref="Entity"/> that contains synchronization-related information.
  /// </summary>
  [HierarchyRoot(Clustered = false), KeyGenerator(KeyGeneratorKind.None)]
  public abstract class SyncInfo : Entity
  {
    private SyncId cachedSyncId;

    private SyncVersion cachedCreatedVersion;
    private SyncVersion cachedChangeVersion;
    private SyncVersion cachedTombstoneVersion;

    private Key cachedSyncTargetKey;

    /// <summary>
    /// Gets the global ID of the item.
    /// </summary>
    [Key, Field(Length = 32)]
    internal string Id { get; private set; }

    /// <summary>
    /// Gets the global ID of the item.
    /// </summary>
    /// <value> The global ID of the item. </value>
    public SyncId SyncId
    {
      get
      {
        if (cachedSyncId==null)
          cachedSyncId = SyncIdBuilder.GetSyncId(Id);
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
    [Field]
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
    public SyncVersion CreationVersion
    {
      get
      {
        if (cachedCreatedVersion==null)
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
    /// <returns>
    /// The version of the most recent change made to the item. Returns a null when the change version has not been set.
    /// </returns>
    /// <exception cref="T:System.ArgumentNullException">An attempt was made to set the value to a null.</exception>
    public SyncVersion ChangeVersion
    {
      get
      {
        if (cachedChangeVersion==null) {
          cachedChangeVersion = new SyncVersion(ChangeReplicaKey, (ulong) ChangeTickCount);
        }

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
        if (cachedTombstoneVersion==null)
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

    [Infrastructure]
    internal abstract Entity SyncTarget { get; }

    [Infrastructure]
    internal Key SyncTargetKey
    {
      get
      {
        if (cachedSyncTargetKey==null)
          cachedSyncTargetKey = SyncTarget!=null ? SyncTarget.Key : GetReferenceKey(TypeInfo.Fields[WellKnown.EntityFieldName]);
        return cachedSyncTargetKey;
      }
      set
      {
        SetReferenceKey(TypeInfo.Fields[WellKnown.EntityFieldName], value);
        cachedSyncTargetKey = value;
      }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncInfo"/> class.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="id">Identifier.</param>
    protected SyncInfo(Session session, SyncId id)
      : base(session, id.ToString())
    {
    }
  }
}