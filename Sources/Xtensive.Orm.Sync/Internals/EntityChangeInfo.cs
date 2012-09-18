using System;
using Xtensive.Orm.Sync.Model;

namespace Xtensive.Orm.Sync
{
  internal sealed class EntityChangeInfo
  {
    public Key Key { get; private set; }

    public EntityChangeKind ChangeKind { get; private set; }

    public EntityChangeInfo(Key key, EntityChangeKind changeKind)
    {
      if (key==null)
        throw new ArgumentNullException("key");

      Key = key;
      ChangeKind = changeKind;
    }
  }
}