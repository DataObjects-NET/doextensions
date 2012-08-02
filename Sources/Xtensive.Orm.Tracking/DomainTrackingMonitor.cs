// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2012.05.16

using System;
using Xtensive.IoC;

namespace Xtensive.Orm.Tracking
{
  /// <summary>
  /// Implementation of <see cref="IDomainTrackingMonitor"/> interface.
  /// </summary>
  [Service(typeof (IDomainTrackingMonitor), Singleton = true)]
  public class DomainTrackingMonitor : IDomainTrackingMonitor
  {
    private readonly Domain domain;
    private int subscriberNumber;
    private object gate = new object();
    private EventHandler<TrackingCompletedEventArgs> trackingCompletedHandler;
    private bool isDisabled;

    /// <summary>
    /// Gets or sets the filter that is applied to include only entities of required types.
    /// </summary>
    public Func<Type, bool> Filter { get; set; }

    /// <summary>
    /// Occurs when a single tracking operation is completed.
    /// </summary>
    public event EventHandler<TrackingCompletedEventArgs> TrackingCompleted
    {
      add {
        lock (gate) {
          if (!HasSubscribers && !isDisabled) {
            Attach();
          }
          trackingCompletedHandler += value;
          subscriberNumber++;
        } 
      }
      remove {
        lock (gate) {
          trackingCompletedHandler -= value;
          subscriberNumber--;
          if (!HasSubscribers)
            Detach();
        }
      }
    }

    /// <summary>
    /// Disables tracking.
    /// </summary>
    public void Disable()
    {
      if (isDisabled)
        return;

      isDisabled = true;
      if (HasSubscribers)
        Detach();
    }

    /// <summary>
    /// Enables tracking.
    /// </summary>
    public void Enable()
    {
      if (!isDisabled)
        return;

      isDisabled = false;
      if (HasSubscribers)
        Attach();
    }

    private bool HasSubscribers
    {
      get { return subscriberNumber > 0; }
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
      if (Filter != null) {
          tm.Filter = Filter;
      }
      tm.TrackingCompleted += OnTrackingCompleted;
    }

    private void OnTrackingCompleted(object sender, TrackingCompletedEventArgs e)
    {
      var handler = trackingCompletedHandler;
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
      if (domain == null)
        return;
      Detach();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainTrackingMonitor"/> class.
    /// </summary>
    /// <param name="domain">The domain.</param>
    [ServiceConstructor]
    public DomainTrackingMonitor(Domain domain)
    {
      this.domain = domain;
    }
  }
}