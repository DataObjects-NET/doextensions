using System;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Represents the metadata that is associated with an item in the synchronization scope.
  /// </summary>
  public class SyncInfo<TEntity> : SyncInfo where TEntity : Entity
  {
    [Field]
    [Association(OnOwnerRemove=OnRemoveAction.None, OnTargetRemove=OnRemoveAction.None)]
    public TEntity Entity { get; private set; }

    public SyncInfo(Session session)
      : base(session)
    {
    }
  }
}
