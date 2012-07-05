using System;
using System.Collections.Generic;

namespace Xtensive.Orm.Tracking
{
  public class TrackingCompletedEventArgs : EventArgs
  {
    public IEnumerable<ITrackingItem> Changes { get; private set; }

    public TrackingCompletedEventArgs(IEnumerable<ITrackingItem> changes)
    {
      if (changes == null)
        throw new ArgumentNullException("changes");

      Changes = changes;
    }
  }
}
