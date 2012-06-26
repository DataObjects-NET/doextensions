using System;
using System.Collections.Generic;

namespace Xtensive.Orm.Sync
{
  public class SyncConfiguration
  {
    public HashSet<Type> Types { get; private set; }

    public SyncConfiguration()
    {
      Types = new HashSet<Type>();
    }
  }
}
