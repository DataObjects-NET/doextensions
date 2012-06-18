using System;

namespace Xtensive.Orm.Tracking
{
  public interface ITrackingMonitor
  {
    event EventHandler<TrackingCompletedEventArgs> TrackingCompleted;

    void Enable();

    void Disable();
  }
}