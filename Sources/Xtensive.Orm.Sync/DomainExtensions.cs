using System.Threading;
using Microsoft.Synchronization;
using Xtensive.Orm.Sync;
using Xtensive.Orm.Sync.Services;

namespace Xtensive.Orm
{
  /// <summary>
  /// <see cref="Domain"/> extensions for Xtensive.Orm.Sync.
  /// </summary>
  public static class DomainExtensions
  {
    /// <summary>
    /// Gets the <see cref="KnowledgeSyncProvider"/> implementation
    /// for the specified <paramref name="domain"/>.
    /// </summary>
    /// <param name="domain">The domain to ge sync provider for.</param>
    /// <returns>Sync provider for the domain if sync is enabled for specified
    /// domain, otherwise null.
    /// </returns>
    public static OrmSyncProvider GetSyncProvider(this Domain domain)
    {
      return domain.Services.Get<OrmSyncProvider>();
    }

    /// <summary>
    /// Suspends the current thread until the thread that is processing the synchronization 
    /// queue has emptied that queue.
    /// </summary>
    /// <param name="domain">The domain.</param>
    public static void WaitForPendingSyncTasks(this Domain domain)
    {
      var syncModule = domain.Extensions.Get<SyncModule>();
      if (syncModule == null)
        return;

      while (syncModule.HasPendingTasks)
        Thread.Sleep(10);
    }
  }
}
