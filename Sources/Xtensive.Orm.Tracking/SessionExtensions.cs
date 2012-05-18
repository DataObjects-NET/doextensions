
using System;
using Xtensive.Orm.Tracking;

namespace Xtensive.Orm
{
  public static class SessionExtensions
  {
    public static void StartTracking(this Session session, Action<TrackingResult> callback)
    {
      var tm = session.Services.Get<SessionTrackingMonitor>();
      tm.Start(callback);
    }

    public static void StopTracking(this Session session)
    {
      var tm = session.Services.Get<SessionTrackingMonitor>();
      tm.Stop();
    }
  }
}
