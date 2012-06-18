using System;

namespace Xtensive.Orm.Sync
{
  public class Identity
  {
    public Guid GlobalId { get; set;}

    public Key Key { get; private set; }

    public Identity(Key key)
    {
      Key = key;
    }

    public Identity(Guid globalId, Key key)
    {
      GlobalId = globalId;
      Key = key;
    }
  }
}
