using System;

namespace Xtensive.Orm.Tracking
{
  public interface ISessionTrackingMonitor : ISessionService, IDisposable
  {
    event EventHandler<TrackingCompletedEventArgs> TrackingCompleted;
  }
}