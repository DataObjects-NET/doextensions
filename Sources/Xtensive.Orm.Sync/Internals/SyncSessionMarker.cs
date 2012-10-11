using System;

namespace Xtensive.Orm.Sync
{
  internal sealed class SyncSessionMarker : IDisposable
  {
    private readonly Session session;
    private bool isDisposed;

    public static IDisposable Add(Session session)
    {
      var result = new SyncSessionMarker(session);
      session.Extensions.Set(result);
      return result;
    }

    public static bool Check(Session session)
    {
      return session.Extensions.Get<SyncSessionMarker>()!=null;
    }

    public void Dispose()
    {
      if (isDisposed)
        return;
      isDisposed = true;
      session.Extensions.Set<SyncSessionMarker>(null);
    }

    private SyncSessionMarker(Session session)
    {
      this.session = session;
    }
  }
}