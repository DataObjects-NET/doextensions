using System;
using System.Collections.Generic;
using Microsoft.Synchronization;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Sync
{
  [Serializable]
  public class ItemChangeData
  {
    public ItemChange Change { get; set; }

    public Identity Identity { get; set; }

    public Tuple Tuple { get; set; }

    public Dictionary<string, Identity> References { get; private set; }

    public ItemChangeData()
    {
      References = new Dictionary<string,Identity>();
    }
  }
}
