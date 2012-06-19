using System;

namespace Xtensive.Orm.Tracking
{
  public interface ISessionTrackingMonitor : ISessionBound, ISessionService, IDisposable, ITrackingMonitor
  {
  }
}