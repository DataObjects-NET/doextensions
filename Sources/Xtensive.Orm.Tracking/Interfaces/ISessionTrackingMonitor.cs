using System;

namespace Xtensive.Orm.Tracking
{
  public interface ISessionTrackingMonitor : ISessionService, IDisposable
  {
    bool IsRunning { get; }

    void Start(Action<TrackingResult> callback);

    void Stop();
  }
}