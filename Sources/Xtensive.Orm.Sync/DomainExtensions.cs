using System.Threading;
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
