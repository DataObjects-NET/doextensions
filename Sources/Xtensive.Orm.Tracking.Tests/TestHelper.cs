using Xtensive.Tuples;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Tracking.Tests
{
  public static class TestHelper
  {
    public static TrackingItem CreateTrackingItem(Key key, TrackingItemState state)
    {
      var tuple = Tuple.Create(typeof (string));
      var diff = new DifferentialTuple(tuple);
      return new TrackingItem(key, diff, state);
    }
  }
}
