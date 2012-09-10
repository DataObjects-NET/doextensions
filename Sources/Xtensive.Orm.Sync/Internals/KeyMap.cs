using System.Collections.Generic;
using Microsoft.Synchronization;
using Xtensive.Orm.Sync.DataExchange;

namespace Xtensive.Orm.Sync
{
  internal sealed class KeyMap
  {
    private readonly Dictionary<Key, Key> keyIndex = new Dictionary<Key, Key>();
    private readonly Dictionary<SyncId, Key> globalIdIndex = new Dictionary<SyncId, Key>();

    public void Register(Identity mapping, Key newKey)
    {
      keyIndex.Add(mapping.Key, newKey);
      globalIdIndex.Add(mapping.GlobalId, newKey);
    }

    public Key Resolve(Identity identity)
    {
      return ResolveByKey(identity.Key) ?? ResolveByGlobalId(identity.GlobalId);
    }

    private Key ResolveByKey(Key key)
    {
      Key result;
      keyIndex.TryGetValue(key, out result);
      return result;
    }

    private Key ResolveByGlobalId(SyncId globalId)
    {
      if (globalId==null)
        return null;

      Key result;
      globalIdIndex.TryGetValue(globalId, out result);
      return result;
    }
  }
}
