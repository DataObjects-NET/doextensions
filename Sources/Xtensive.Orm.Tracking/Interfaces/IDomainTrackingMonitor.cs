using System;

namespace Xtensive.Orm.Tracking
{
  /// <summary>
  /// Marker interface for domain version of <see cref="ITrackingMonitor"/>.
  /// </summary>
  public interface IDomainTrackingMonitor : IDomainService, IDisposable, ITrackingMonitor
  {
  }
}