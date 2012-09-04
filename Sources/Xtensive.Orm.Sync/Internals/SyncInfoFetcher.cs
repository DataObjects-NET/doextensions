using System;
using System.Linq;
using Xtensive.Caching;
using Xtensive.IoC;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (SyncInfoFetcher), Singleton = true)]
  internal sealed class SyncInfoFetcher : ISessionService
  {
    private readonly Session session;
    private readonly ICache<Guid, SyncInfo> cache;

    public SyncInfo Fetch(Guid globalId)
    {
      SyncInfo result;

      if (!cache.TryGetItem(globalId, true, out result)) {
        result = session.Query.Execute(q => q.All<SyncInfo>().FirstOrDefault(s => s.GlobalId==globalId));
        if (result!=null)
          cache.Add(result);
      }

      return result;
    }

    [ServiceConstructor]
    public SyncInfoFetcher(Session session)
    {
      if (session==null)
        throw new ArgumentNullException("session");

      this.session = session;

      cache = new LruCache<Guid, SyncInfo>(
        WellKnown.SyncInfoCacheSize, s => s.GlobalId,
        new WeakCache<Guid, SyncInfo>(false, s => s.GlobalId));
    }
  }
}