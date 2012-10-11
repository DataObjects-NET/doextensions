using Xtensive.Orm.Sync;

namespace Xtensive.Orm
{
  /// <summary>
  /// <see cref="Domain"/> extensions for Xtensive.Orm.Sync.
  /// </summary>
  public static class DomainExtensions
  {
    /// <summary>
    /// Gets the <see cref="SyncManager"/> for the specified <paramref name="domain"/>.
    /// </summary>
    /// <param name="domain">The domain to use.</param>
    /// <returns>Sync manager for specified domain if sync is enabled, otherwise null.</returns>
    public static ISyncManager GetSyncManager(this Domain domain)
    {
      return domain.Services.Get<ISyncManager>();
    }
  }
}
