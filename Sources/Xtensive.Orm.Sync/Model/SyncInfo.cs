using Microsoft.Synchronization;
using Xtensive.Aspects;

namespace Xtensive.Orm.Sync.Model
{
  /// <summary>
  /// <see cref="Entity"/> that contains synchronization-related information.
  /// </summary>
  [HierarchyRoot(Clustered = false), KeyGenerator(KeyGeneratorKind.None)]
  public abstract class SyncInfo : Entity
  {
    private SyncId cachedSyncId;
    private Key cachedTargetKey;

    /// <summary>
    /// Gets the global ID of the item.
    /// This is a primary key.
    /// </summary>
    [Key, Field(Length = 32)]
    public string Id { get; private set; }

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
    /// <returns>True if the item has been deleted; otherwise, false.</returns>
    [Field]
    public bool IsTombstone { get; set; }

    /// <summary>
    /// Gets or sets creation version.
    /// </summary>
    [Field]
    public SyncVersionData CreationVersion { get; set; }

    /// <summary>
    /// Gets or sets change version.
    /// </summary>
    [Field]
    public SyncVersionData ChangeVersion { get; set; }

    [Infrastructure]
    internal abstract Entity Target { get; }

    [Infrastructure]
    internal Key TargetKey
    {
      get
      {
        if (cachedTargetKey==null)
          cachedTargetKey = Target!=null ? Target.Key : GetReferenceKey(TypeInfo.Fields[WellKnown.EntityFieldName]);
        return cachedTargetKey;
      }
      set
      {
        SetReferenceKey(TypeInfo.Fields[WellKnown.EntityFieldName], value);
        cachedTargetKey = value;
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