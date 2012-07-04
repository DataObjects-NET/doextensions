
namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Helper for fluent configuration of <see cref="OrmSyncProvider"/>
  /// </summary>
  public interface ISkippable
  {
    /// <summary>
    /// Skips all instances of <typeparamref name="TEntity"/> in sync session.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <returns></returns>
    ISkippable Skip<TEntity>() where TEntity : Entity;
  }
}
