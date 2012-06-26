using Xtensive.Orm.Sync;

namespace Xtensive.Orm
{
  public static class DomainExtensions
  {
    public static SyncProviderWrapper GetSyncProvider(this Domain domain)
    {
      return domain.Services.Get<SyncProviderWrapper>();
    }
  }
}
