using System;
using Xtensive.Aspects;

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

    internal override Entity GetEntity()
    {
      return Entity;
    }

    internal override Type GetEntityType()
    {
      return typeof(TEntity);
    }

    public SyncInfo(Session session)
      : base(session)
    {
    }
  }
}
