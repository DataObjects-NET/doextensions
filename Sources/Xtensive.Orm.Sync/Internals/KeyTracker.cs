using System;
using System.Collections.Generic;
using System.Linq;

namespace Xtensive.Orm.Sync
{
  internal sealed class KeyTracker
  {
    private readonly SyncConfiguration configuration;
    private readonly HashSet<Key> sentKeys = new HashSet<Key>();
    private readonly HashSet<Key> requestedKeys = new HashSet<Key>();

    public bool HasKeysToSync { get { return requestedKeys.Count > 0; } }

    public IEnumerable<Key> GetKeysToSync()
    {
      return requestedKeys.ToList();
    }

    public void RegisterKeySync(Key key)
    {
      if (!TypeIsFilteredOrSkipped(key.TypeReference.Type.GetRoot().UnderlyingType))
        return;
      sentKeys.Add(key);
      requestedKeys.Remove(key);
    }

    public void RequestKeySync(Key key)
    {
      if (!TypeIsFilteredOrSkipped(key.TypeReference.Type.GetRoot().UnderlyingType))
        return;
      if (sentKeys.Contains(key))
        return;
      requestedKeys.Add(key);
    }

    public void UnrequestKeySync(Key key)
    {
      requestedKeys.Remove(key);
    }

    private bool TypeIsFilteredOrSkipped(Type type)
    {
      if (configuration.Filters.ContainsKey(type))
        return true;
      if (configuration.SkipTypes.Contains(type))
        return true;
      if (configuration.SyncAll)
        return false;
      return !configuration.SyncTypes.Contains(type);
    }

    public KeyTracker(SyncConfiguration configuration)
    {
      if (configuration==null)
        throw new ArgumentNullException("configuration");
      this.configuration = configuration;
    }
  }
}