using System;
using System.Collections.Generic;

namespace Xtensive.Orm.Tracking
{
  public class TrackingCompletedEventArgs : EventArgs
  {
    public IEnumerable<ITrackingItem> TrackingItems { get; private set; }

    public TrackingCompletedEventArgs(IEnumerable<ITrackingItem> items)
    {
      if (items == null)
        throw new ArgumentNullException("items");

      TrackingItems = items;
    }
  }
}