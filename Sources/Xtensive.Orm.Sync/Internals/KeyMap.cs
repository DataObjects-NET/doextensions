using System;
using System.Collections.Generic;
using System.IO;
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
      Key existingKey;

      if (keyIndex.TryGetValue(mapping.Key, out existingKey))
        throw MappingConflict(mapping.Key.Format(), newKey.Format(), existingKey.Format());

      if (globalIdIndex.TryGetValue(mapping.GlobalId, out existingKey))
        throw MappingConflict(mapping.GlobalId.ToString(), newKey.Format(), existingKey.Format());

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

    private static InvalidOperationException MappingConflict(string from, string to, string existing)
    {
      return new InvalidOperationException(string.Format(
        "Unable to register key mapping from '{0}' to '{1}', '{0} is already mapped to '{2}'.",
        from, to, existing));
    }
  }
}
