using System;

namespace Xtensive.Orm.Tracking
{
  public class TrackingCompletedEventArgs : EventArgs
  {
    public TrackingResult Result { get; private set; }

    public TrackingCompletedEventArgs(TrackingResult result)
    {
      if (result == null)
        throw new ArgumentNullException("result");

      Result = result;
    }
  }
}
