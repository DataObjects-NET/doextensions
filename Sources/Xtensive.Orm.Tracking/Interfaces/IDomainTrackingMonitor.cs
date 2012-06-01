using System;

namespace Xtensive.Orm.Tracking
{
  public interface IDomainTrackingMonitor : IDomainService, IDisposable, ITrackingMonitor
  {
  }
}