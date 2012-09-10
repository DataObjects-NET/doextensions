using System;
using Microsoft.Synchronization;
using Xtensive.Aspects;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Represents the metadata that is associated with an item in the synchronization scope.
  /// </summary>
  public class SyncInfo<TEntity> : SyncInfo
    where TEntity : IEntity
  {
    /// <summary>
    /// Gets the entity.
    /// </summary>
    [Field]
    [Association(OnOwnerRemove = OnRemoveAction.None, OnTargetRemove = OnRemoveAction.None)]
    public TEntity Entity { get; private set; }

    [Infrastructure]
    internal override Entity SyncTarget
    {
      get { return (Entity) (object) Entity; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncInfo&lt;TEntity&gt;"/> class.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="id">Identifier.</param>
    public SyncInfo(Session session, SyncId id)
      : base(session, id)
    {
    }
  }
}
