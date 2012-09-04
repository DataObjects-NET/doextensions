using System;
using System.Linq;
using Xtensive.Caching;

namespace Xtensive.Orm.Sync
{
  internal sealed class SyncInfoCache
  {
    private readonly Session session;
    private readonly ICache<Guid, SyncInfo> cache;

    public SyncInfo Get(Guid globalId)
    {
      SyncInfo result;

      if (!cache.TryGetItem(globalId, true, out result)) {
        result = session.Query.Execute(q => q.All<SyncInfo>().FirstOrDefault(s => s.GlobalId==globalId));
        if (result!=null)
          cache.Add(result);
      }

      return result;
    }

    public SyncInfoCache(Session session)
    {
      if (session==null)
        throw new ArgumentNullException("session");

      this.session = session;
      cache = new LruCache<Guid, SyncInfo>(
        Wellknown.SyncInfoCacheSize, s => s.GlobalId,
        new WeakCache<Guid, SyncInfo>(false, s => s.GlobalId));
    }
  }
}