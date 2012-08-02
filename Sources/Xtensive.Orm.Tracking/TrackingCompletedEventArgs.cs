using System;
using System.Collections.Generic;

namespace Xtensive.Orm.Tracking
{
  /// <summary>
  /// Event arguments for <see cref="ITrackingMonitor.TrackingCompleted"/> event.
  /// </summary>
  public class TrackingCompletedEventArgs : EventArgs
  {
    /// <summary>
    /// Gets the changes.
    /// </summary>
    public IEnumerable<ITrackingItem> Changes { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackingCompletedEventArgs"/> class.
    /// </summary>
    /// <param name="changes">The changes.</param>
    public TrackingCompletedEventArgs(IEnumerable<ITrackingItem> changes)
    {
      if (changes == null)
        throw new ArgumentNullException("changes");

      Changes = changes;
    }
  }
}
