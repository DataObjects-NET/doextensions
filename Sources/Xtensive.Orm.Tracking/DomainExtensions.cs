
using System;
using Xtensive.Orm.Tracking;

namespace Xtensive.Orm
{
  public static class DomainExtensions
  {
    public static void StartTracking(this Domain domain, Action<TrackingResult> callback)
    {
      var tm = domain.Services.Get<DomainTrackingMonitor>();
      tm.Start(callback);
    }

    public static void StopTracking(this Domain domain)
    {
      var tm = domain.Services.Get<DomainTrackingMonitor>();
      tm.Stop();
    }
  }
}
