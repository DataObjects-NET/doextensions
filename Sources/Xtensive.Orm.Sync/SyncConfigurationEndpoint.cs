using System;
using System.Linq.Expressions;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Endpoint for fluent configuration of <see cref="OrmSyncProvider"/> instance.
  /// </summary>
  public class SyncConfigurationEndpoint : IFilterable
  {
    private readonly SyncConfiguration configuration;

    /// <summary>
    /// Includes all instances of any types in sync session.
    /// </summary>
    public IFilterable All()
    {
      configuration.SyncAll = true;
      return this;
    }

    /// <summary>
    /// Resets this instance.
    /// </summary>
    /// <returns></returns>
    public IFilterable Reset()
    {
      configuration.Reset();
      return this;
    }

    /// <summary>
    /// Gets or sets the size of the synchronization batch.
    /// </summary>
    public int BatchSize
    {
      get { return configuration.BatchSize; }
      set { configuration.BatchSize = value; }
    }

    #region Implementation of IFilterable

    /// <summary>
    /// Includes all instances of <typeparamref name="TEntity"/> in sync session.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <returns></returns>
    public IFilterable All<TEntity>() where TEntity : Entity
    {
      var candidate = typeof (TEntity);
      CheckIsHierarchyRoot(candidate);

      if (!configuration.SyncAll)
        configuration.SyncTypes.Add(candidate);

      return this;
    }

    /// <summary>
    /// Includes all instances of <typeparamref name="TEntity"/> in sync session and apply <paramref name="filter"/> to each of them.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="filter">The filter to be applied to entity.</param>
    /// <returns></returns>
    public IFilterable All<TEntity>(Expression<Func<TEntity, bool>> filter) where TEntity : Entity
    {
      if (filter == null)
        throw new ArgumentNullException("filter");

      var candidate = typeof (TEntity);
      CheckIsHierarchyRoot(candidate);

      Expression existing;
      if (configuration.Filters.TryGetValue(candidate, out existing))
        throw new InvalidOperationException(string.Format("Filter for {0} is already registered", candidate.Name));
      
      if (!configuration.SyncAll)
        configuration.SyncTypes.Add(candidate);
      configuration.Filters[candidate] = filter;
      return this;
    }

    #endregion

    #region Implementation of ISkippable

    /// <summary>
    /// Skips all instances of <typeparamref name="TEntity"/> in sync session.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <returns></returns>
    ISkippable ISkippable.Skip<TEntity>()
    {
      var candidate = typeof (TEntity);
      CheckIsHierarchyRoot(candidate);

      configuration.SkipTypes.Add(candidate);
      configuration.SyncTypes.Remove(candidate);
      if (configuration.Filters.ContainsKey(candidate))
        configuration.Filters.Remove(candidate);
      return this;
    }

    #endregion

    private static void CheckIsHierarchyRoot(Type candidate)
    {
      var attribute = Attribute.GetCustomAttribute(candidate, typeof(HierarchyRootAttribute));
      if (attribute == null)
        throw new InvalidOperationException(string.Format("{0} is not a hierarchy root", candidate.Name));
    }

    internal SyncConfigurationEndpoint(SyncConfiguration configuration)
    {
      this.configuration = configuration;
    }
  }
}
