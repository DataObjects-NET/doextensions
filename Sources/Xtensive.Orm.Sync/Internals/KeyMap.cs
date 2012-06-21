using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Orm.Services;

namespace Xtensive.Orm.Sync
{
  internal class KeyMap : SessionBound
  {
    private readonly Dictionary<Key, Key> keyIndex = new Dictionary<Key,Key>();
    private readonly Dictionary<Guid, Key> globalIdIndex = new Dictionary<Guid,Key>();
    private readonly DirectEntityAccessor accessor;
    private readonly SyncRootSet syncRoots;

    public void Register(Identity mapping, Key newKey)
    {
      keyIndex.Add(mapping.Key, newKey);
      globalIdIndex.Add(mapping.GlobalId, newKey);
    }

//    public Key this [Key key]
//    {
//      get { return FindByKey(key); }
//    }
//
//    public Key this [Guid globalId]
//    {
//      get { return FindByGlobalId(globalId); }
//    }

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

    private Key ResolveByGlobalId(Guid globalId)
    {
      if (globalId == Guid.Empty)
        return null;

      Key result;
      if (globalIdIndex.TryGetValue(globalId, out result))
        return result;

      var info = Session.Query.All<SyncInfo>()
        .SingleOrDefault(i => i.GlobalId == globalId);

      if (info == null)
        return null;

      var entityType = info.SyncTargetType;
      var syncRoot = syncRoots[entityType];
      return accessor.GetReferenceKey(info, syncRoot.EntityField);
    }

    public KeyMap(Session session, SyncRootSet syncRoots)
      : base(session)
    {
      accessor = session.Services.Get<DirectEntityAccessor>();
      this.syncRoots = syncRoots;
    }
  }
}
