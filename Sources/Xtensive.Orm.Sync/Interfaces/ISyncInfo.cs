using System;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  public interface ISyncInfo : IEntity
  {
    /// <summary>
    /// Gets the id.
    /// </summary>
    [Field, Key]
    long Id { get; }

    /// <summary>
    /// Gets the global ID of the item.
    /// </summary>
    [Field]
    Guid GlobalId { get; }

    /// <summary>
    /// Gets the global ID of the item.
    /// </summary>
    /// <value> The global ID of the item. </value>
    SyncId SyncId { get; }

    /// <summary>
    /// Gets or sets a value that indicates whether the item has been deleted from the item store.
    /// </summary>
    /// 
    /// <returns>
    /// true if the item has been deleted; otherwise, false.
    /// </returns>
    [Field]
    bool IsTombstone { get; set; }

    /// <summary>
    /// Gets or sets the creation version for the item.
    /// </summary>
    /// <value> The creation version for the item. </value>
    /// <exception cref="T:System.ArgumentNullException">An attempt was made to set the value to a null.</exception>
    SyncVersion CreationVersion { get; set; }

    /// <summary>
    /// Gets or sets the version of the most recent change made to the item.
    /// </summary>
    /// 
    /// <returns>
    /// The version of the most recent change made to the item. Returns a null when the change version has not been set.
    /// </returns>
    /// <exception cref="T:System.ArgumentNullException">An attempt was made to set the value to a null.</exception>
    SyncVersion ChangeVersion { get; set; }

    /// <summary>
    /// Gets or sets the version when the item was deleted.
    /// </summary>
    /// <value> The version when the item was deleted. </value>
    /// <exception cref="T:System.ArgumentNullException">An attempt was made to set the value to a null.</exception>
    SyncVersion TombstoneVersion { get; set; }
  }
}
