using System;
using System.Collections.Generic;
using Xtensive.Orm.Sync.Model;

namespace Xtensive.Orm.Sync
{
  internal sealed class MetadataSet
  {
    private readonly Dictionary<Key, SyncInfo> index = new Dictionary<Key, SyncInfo>();

    public SyncInfo this[Key key]
    {
      get
      {
        if (key==null)
          throw new ArgumentNullException("key");
        SyncInfo result;
        index.TryGetValue(key, out result);
        return result;
      }
    }

    public void Add(SyncInfo metadata)
    {
      if (metadata==null)
        throw new ArgumentNullException("metadata");
      index[metadata.TargetKey] = metadata;
    }
  }
}