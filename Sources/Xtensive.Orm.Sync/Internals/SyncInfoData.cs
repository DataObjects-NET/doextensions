using System;
using System.Xml.Serialization;
using Xtensive.Tuples;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Sync
{
  [Serializable]
  public class SyncInfoData
  {
    public string EntityKey { get; set; }

    public string EntityData { get; set; }
  }
}
