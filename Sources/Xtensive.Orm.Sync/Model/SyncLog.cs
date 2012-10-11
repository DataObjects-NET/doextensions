using System;

namespace Xtensive.Orm.Sync.Model
{
  /// <summary>
  /// Log entry with information about changed entity.
  /// </summary>
  [HierarchyRoot(Clustered = false), KeyGenerator(Name = WellKnown.TickGeneratorName)]
  public sealed class SyncLog : Entity
  {
    /// <summary>
    /// Gets tick when change occured.
    /// This field is a primary key.
    /// </summary>
    [Key, Field]
    public long Tick { get; private set; }

    /// <summary>
    /// Gets key of entity that was changed.
    /// </summary>
    [Field]
    public SyncKey TargetKey { get; private set; }

    /// <summary>
    /// Gets <see cref="EntityChangeKind" /> for changed entity.
    /// </summary>
    [Field]
    public EntityChangeKind ChangeKind { get; private set; }

    /// <summary>
    /// Creates new instance of <see cref="SyncLog"/> class.
    /// </summary>
    /// <param name="session">Session.</param>
    /// <param name="entityKey">Key of changed entity.</param>
    /// <param name="changeKind">Kind change.</param>
    public SyncLog(Session session, Key entityKey, EntityChangeKind changeKind)
      : base(session)
    {
      if (entityKey==null)
        throw new ArgumentNullException("entityKey");

      TargetKey = new SyncKey(session, entityKey);
      ChangeKind = changeKind;
    }
  }
}