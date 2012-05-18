using System;

namespace Xtensive.Orm.Tracking
{
  public interface IDomainTrackingMonitor : IDomainService, IDisposable
  {
    bool IsRunning { get; }
    void Start(Action<TrackingResult> callback);
    void Stop();
  }
}