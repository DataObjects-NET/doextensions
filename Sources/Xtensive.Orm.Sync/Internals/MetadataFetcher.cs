using System;
using System.Linq;
using Microsoft.Synchronization;
using Xtensive.Caching;
using Xtensive.IoC;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (MetadataFetcher), Singleton = true)]
  internal sealed class MetadataFetcher : ISessionService
  {
    private readonly Session session;
    private readonly ICache<SyncId, SyncInfo> cache;

    public SyncInfo GetMetadata(SyncId globalId)
    {
      SyncInfo result;

      var globalIdString = globalId.ToString();

      if (!cache.TryGetItem(globalId, true, out result)) {
        result = session.Query.Execute(q => q.All<SyncInfo>().FirstOrDefault(s => s.Id==globalIdString));
        if (result!=null)
          cache.Add(result);
      }

      return result;
    }

    [ServiceConstructor]
    public MetadataFetcher(Session session)
    {
      if (session==null)
        throw new ArgumentNullException("session");

      this.session = session;

      cache = new LruCache<SyncId, SyncInfo>(
        WellKnown.SyncInfoCacheSize, s => s.SyncId,
        new WeakCache<SyncId, SyncInfo>(false, s => s.SyncId));
    }
  }
}