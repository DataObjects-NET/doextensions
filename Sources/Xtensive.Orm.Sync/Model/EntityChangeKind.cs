namespace Xtensive.Orm.Sync.Model
{
  /// <summary>
  /// Type of entity change.
  /// </summary>
  public enum EntityChangeKind
  {
    /// <summary>
    /// Entity is created.
    /// </summary>
    Create = 0,

    /// <summary>
    /// Entity is updated.
    /// </summary>
    Update = 1,

    /// <summary>
    /// Entity is removed.
    /// </summary>
    Remove = 2,
  }
}