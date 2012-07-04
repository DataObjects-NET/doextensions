using System;
using System.Linq.Expressions;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Helper for fluent configuration of <see cref="OrmSyncProvider"/>
  /// </summary>
  public interface IFilterable : ISkippable
  {
    /// <summary>
    /// Includes all instances of <typeparamref name="TEntity"/> in sync session.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <returns></returns>
    IFilterable All<TEntity>() where TEntity : Entity;

    /// <summary>
    /// Includes all instances of <typeparamref name="TEntity"/> in sync session and apply <paramref name="filter"/> to each of them.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="filter">The filter to be applied to entity.</param>
    IFilterable All<TEntity>(Expression<Func<TEntity, bool>> filter) where TEntity : Entity;
  }
}
