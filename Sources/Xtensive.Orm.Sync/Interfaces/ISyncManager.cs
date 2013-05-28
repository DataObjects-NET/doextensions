using System;
using System.Collections.Generic;
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
    /// Gets list of hierarchy roots that could be synchronized.
    /// </summary>
    IList<Type> SynchronizedRoots { get; }

    /// <summary>
    /// Creates <see cref="OrmSyncProvider"/> for current domain.
    /// </summary>
    /// <returns>New <see cref="OrmSyncProvider"/> for current domain.</returns>
    OrmSyncProvider GetSyncProvider();

    /// <summary>
    /// Updates metadata by applying sync log.
    /// Not all sync log entries are processed at once.
    /// To update all metadata callers should invoke
    /// this method multiple times until it returns false.
    /// <returns>True if more metadata might require updating,
    /// false if all metadata has been processed.</returns>
    /// </summary>
    bool UpdateMetadataOnce();

    /// <summary>
    /// Updates metadata by applying sync log.
    /// Unlike <see cref="UpdateMetadataOnce"/> method
    /// this method updates all metadata in single call.
    /// </summary>
    void UpdateMetadata();

    /// <summary>
    /// Starts metadata processor in current domain.
    /// If multiple domains use the same database it's recommended
    /// to have single metadata processor.
    /// </summary>
    void StartMetadataProcessor();

    /// <summary>
    /// Waits for pending metadata update tasks in current domain.
    /// </summary>
    void WaitForPendingSyncTasks();

    /// <summary>
    /// Checks if sync provider is using specified <paramref name="session"/>.
    /// </summary>
    /// <param name="session">Session to check.</param>
    /// <returns>True if sync is using <paramref name="session"/>,
    /// otherwise false.</returns>
    bool IsSyncRunning(Session session);

    /// <summary>
    /// Deletes all metadata for the specified type.
    /// </summary>
    /// <param name="session">Session to use.</param>
    /// <param name="type">Type to delete metadata for (hierarchy root).</param>
    void ForgetMetadata(Session session, Type type);

    /// <summary>
    /// Creates metadata information for the entities that don't have it
    /// for some reason.
    /// </summary>
    /// <param name="session">Session to use.</param>
    /// <param name="type">Type to create metadata for (hierarchy root).</param>
    void CreateMissingMetadata(Session session, Type type);
  }
}