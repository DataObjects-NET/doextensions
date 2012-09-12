using System;
using Xtensive.IoC;

namespace Xtensive.Orm.Tracking
{
  [Service(typeof (IDomainTrackingMonitor), Singleton = true)]
  internal sealed class DomainTrackingMonitor : IDomainTrackingMonitor, IDomainService, IDisposable
  {
    private readonly object gate = new object();
    private readonly Domain domain;

    private bool isDisabled;
    private int subscriberNumber;

    private event EventHandler<TrackingCompletedEventArgs> TrackingCompletedHandler;

    private bool HasSubscribers
    {
      get { return subscriberNumber > 0; }
    }

    public event EventHandler<TrackingCompletedEventArgs> TrackingCompleted
    {
      add {
        lock (gate) {
          if (!HasSubscribers && !isDisabled)
            Attach();
          TrackingCompletedHandler += value;
          subscriberNumber++;
        } 
      }
      remove {
        lock (gate) {
          TrackingCompletedHandler -= value;
          subscriberNumber--;
          if (!HasSubscribers)
            Detach();
        }
      }
    }

    public void Disable()
    {
      if (isDisabled)
        return;

      isDisabled = true;
      if (HasSubscribers)
        Detach();
    }

    public void Enable()
    {
      if (!isDisabled)
        return;

      isDisabled = false;
      if (HasSubscribers)
        Attach();
    }

    private void Attach()
    {
      domain.SessionOpen += OnOpenSession;
    }

    private void Detach()
    {
      domain.SessionOpen -= OnOpenSession;
    }

    private void OnOpenSession(object sender, SessionEventArgs args)
    {
      var session = args.Session;

      if (session.Configuration.Type == Configuration.SessionType.KeyGenerator)
        return;
      if (session.Configuration.Type == Configuration.SessionType.System)
        return;

      session.Events.Disposing += OnDisposeSession;
      var tm = session.Services.Get<ISessionTrackingMonitor>();
      tm.TrackingCompleted += OnTrackingCompleted;
    }

    private void OnTrackingCompleted(object sender, TrackingCompletedEventArgs e)
    {
      var handler = TrackingCompletedHandler;
      handler.Invoke(this, e);
    }

    private void OnDisposeSession(object sender, EventArgs e)
    {
      if (sender==null)
        return;

      var session = ((SessionEventAccessor) sender).Session;
      session.Events.Disposing -= OnDisposeSession;
      var tm = session.Services.Get<ISessionTrackingMonitor>();
      tm.TrackingCompleted -= OnTrackingCompleted;
    }

    void IDisposable.Dispose()
    {
      if (domain==null)
        return;
      Detach();
    }

    [ServiceConstructor]
    public DomainTrackingMonitor(Domain domain)
    {
      if (domain==null)
        throw new ArgumentNullException("domain");
      this.domain = domain;
    }
  }
}