using System;

namespace Xtensive.Orm.Tracking
{
  /// <summary>
  /// Base tracking monitor interface
  /// </summary>
  public interface ITrackingMonitor
  {
    /// <summary>
    /// Occurs when a single tracking operation is completed.
    /// </summary>
    event EventHandler<TrackingCompletedEventArgs> TrackingCompleted;

    /// <summary>
    /// Enables tracking.
    /// </summary>
    void Enable();

    /// <summary>
    /// Disables tracking.
    /// </summary>
    void Disable();

    /// <summary>
    /// Gets or sets the filter that is applied to include only entities of required types.
    /// </summary>
    Func<Type, bool> Filter { get; set; }
  }
}