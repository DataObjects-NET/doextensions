using System;
using System.Collections.Generic;

namespace Xtensive.Orm.Tracking
{
  public class TrackingResult
  {
    private IEnumerable<ITrackingItem> items;

    public IEnumerable<ITrackingItem> GetChanges()
    {
      return items;
    }

    public TrackingResult(IEnumerable<ITrackingItem> items)
    {
      if (items == null)
        throw new ArgumentNullException("items");

      this.items = items;
    }
  }
}