using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Provides public API for sync extension.
  /// </summary>
  public interface ISyncManager
  {
    /// <summary>
    /// Gets replica identifier for current domain.
    /// </summary>
    SyncId ReplicaId { get; }

    /// <summary>
    /// Creates <see cref="OrmSyncProvider"/> for current domain.
    /// </summary>
    /// <returns>New <see cref="OrmSyncProvider"/> for current domain.</returns>
    OrmSyncProvider GetSyncProvider();

    /// <summary>
    /// Waits for pending metadata update tasks in this domain.
    /// </summary>
    void WaitForPendingSyncTasks();

    /// <summary>
    /// Starts metadata processor in this domain.
    /// If multiple domains use the same database it's recommended
    /// to have single metadata processor.
    /// </summary>
    void StartMetadataProcessor();

    /// <summary>
    /// Checks if sync provider is using specified <paramref name="session"/>.
    /// </summary>
    /// <param name="session">Session to check.</param>
    /// <returns>True if sync is using <paramref name="session"/>,
    /// otherwise false.</returns>
    bool IsSyncRunning(Session session);
  }
}