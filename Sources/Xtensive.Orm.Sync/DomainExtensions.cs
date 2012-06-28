using Microsoft.Synchronization;
using Xtensive.Orm.Sync;

namespace Xtensive.Orm
{
  /// <summary>
  /// <see cref="Domain"/> extensions for Xtensive.Orm.Sync.
  /// </summary>
  public static class DomainExtensions
  {
    /// <summary>
    /// Gets the <see cref="KnowledgeSyncProvider"/> implementation.
    /// </summary>
    /// <param name="domain">The domain.</param>
    /// <returns></returns>
    public static SyncProviderWrapper GetSyncProvider(this Domain domain)
    {
      return domain.Services.Get<SyncProviderWrapper>();
    }
  }
}
