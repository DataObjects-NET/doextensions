// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2012.05.16

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xtensive.IoC;
using Xtensive.Orm.Internals;

namespace Xtensive.Orm.Tracking
{
  [Service(typeof (TrackingMonitor), Singleton = true)]
  public class TrackingMonitor : IDomainService, IDisposable
  {
    private static readonly PropertyInfo registryAccessor = typeof (Session).GetProperty("EntityChangeRegistry", BindingFlags.Instance | BindingFlags.NonPublic);
    private Domain domain;

    public event EventHandler<TrackingCompletedEventArgs> TrackingCompleted;

    private void OnOpenSession(object sender, SessionEventArgs args)
    {
      var session = args.Session;

      if (session.Configuration.Type == Configuration.SessionType.KeyGenerator)
        return;

      session.Events.Persisting += OnPersisting;
      session.Events.TransactionOpened += OnOpenTransaction;
      session.Events.TransactionCommitted += OnCommitTransaction;
      session.Events.TransactionRollbacked += OnRollBackTransaction;
      session.Events.Disposing += OnDisposeSession;
      var stack = new TrackingStack();
      stack.Push(new TrackingStackFrame());
      session.Extensions.Set(stack);
    }

    private void OnDisposeSession(object sender, EventArgs e)
    {
      if (sender==null)
        return;

      var session = ((SessionEventAccessor) sender).Session;
      session.Events.Persisting -= OnPersisting;
      session.Events.TransactionOpened -= OnOpenTransaction;
      session.Events.TransactionCommitted -= OnCommitTransaction;
      session.Events.TransactionRollbacked -= OnRollBackTransaction;
    }

    private void OnOpenTransaction(object sender, TransactionEventArgs e)
    {
      var session = e.Transaction.Session;
      var stack = session.Extensions.Get<TrackingStack>();
      if (stack==null)
        return;

      stack.Push(new TrackingStackFrame());
    }

    private void OnCommitTransaction(object sender, TransactionEventArgs e)
    {
      var session = e.Transaction.Session;
      var stack = session.Extensions.Get<TrackingStack>();
      if (stack==null)
        return;

      var source = stack.Pop();
      var target = stack.Peek();
      target.MergeWith(source);

      if (e.Transaction.IsNested)
        return;

      if (TrackingCompleted == null)
        return;

      var items = target.ToList() as IEnumerable<ITrackingItem>;
      target.Clear();
      TrackingCompleted.Invoke(this, new TrackingCompletedEventArgs(items));
    }

    private void OnRollBackTransaction(object sender, TransactionEventArgs e)
    {
      var session = e.Transaction.Session;
      var stack = session.Extensions.Get<TrackingStack>();
      if (stack==null)
        return;
      stack.Pop();
    }

    private void OnPersisting(object sender, EventArgs e)
    {
      var accessor = (SessionEventAccessor) sender;
      var session = accessor.Session;
      var registry = GetEntityChangeRegistry(session);
      if (registry.Count==0)
        return;

      var stack = session.Extensions.Get<TrackingStack>();
      if (stack==null)
        return;
      var frame = stack.Peek();

      foreach (var state in registry.GetItems(PersistenceState.Removed))
        frame.Register(new TrackingItem(state.Key, state.DifferentialTuple, TrackingItemState.Removed));

      foreach (var state in registry.GetItems(PersistenceState.New))
        frame.Register(new TrackingItem(state.Key, state.DifferentialTuple, TrackingItemState.Created));

      foreach (var state in registry.GetItems(PersistenceState.Modified))
        frame.Register(new TrackingItem(state.Key, state.DifferentialTuple, TrackingItemState.Modified));
    }

    private static EntityChangeRegistry GetEntityChangeRegistry(Session session)
    {
      return (EntityChangeRegistry) registryAccessor.GetValue(session, null);
    }

    void IDisposable.Dispose()
    {
      if (domain != null)
        domain.SessionOpen -= OnOpenSession;
    }

    [ServiceConstructor]
    public TrackingMonitor(Domain domain)
    {
      this.domain = domain;
      domain.SessionOpen += OnOpenSession;
    }
  }
}