using System;
using System.Collections.Generic;

namespace Xtensive.Orm.Tracking
{
  public class TrackingResult
  {
    public IEnumerable<ITrackingItem> Items { get; private set; }

    public TrackingResult(IEnumerable<ITrackingItem> items)
    {
      if (items == null)
        throw new ArgumentNullException("items");

      Items = items;
    }
  }
}