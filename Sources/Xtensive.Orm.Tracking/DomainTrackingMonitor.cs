// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2012.05.16

using System;
using Xtensive.IoC;

namespace Xtensive.Orm.Tracking
{
  [Service(typeof (DomainTrackingMonitor), Singleton = true)]
  public class DomainTrackingMonitor : IDomainTrackingMonitor
  {
    private readonly Domain domain;
    private bool isRunning;
    private Action<TrackingResult> callbackAction;

    public bool IsRunning { get { return isRunning; } }

    public void Start(Action<TrackingResult> callback)
    {
      if (isRunning)
        return;
      callbackAction = callback;
      Attach();
    }

    public void Stop()
    {
      isRunning = false;
      Detach();
      callbackAction = null;
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
      var tm = session.Services.Get<SessionTrackingMonitor>();
      tm.Start(OnTrackingCompleted);
    }

    private void OnTrackingCompleted(TrackingResult e)
    {
      if (!isRunning)
        return;

      callbackAction.Invoke(e);
    }

    private void OnDisposeSession(object sender, EventArgs e)
    {
      if (sender==null)
        return;

      var session = ((SessionEventAccessor) sender).Session;
      session.Events.Disposing -= OnDisposeSession;
      var tm = session.Services.Get<SessionTrackingMonitor>();
      tm.Stop();
    }

    void IDisposable.Dispose()
    {
      if (domain == null)
        return;
      Detach();
    }

    [ServiceConstructor]
    public DomainTrackingMonitor(Domain domain)
    {
      this.domain = domain;
    }
  }
}