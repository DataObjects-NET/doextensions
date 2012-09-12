using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xtensive.IoC;
using Xtensive.Orm.Internals;

namespace Xtensive.Orm.Tracking
{
  [Service(typeof (ISessionTrackingMonitor), Singleton = true)]
  internal sealed class SessionTrackingMonitor : TrackingMonitor, ISessionTrackingMonitor, ISessionService
  {
    private static readonly PropertyInfo RegistryAccessor = typeof (Session).GetProperty("EntityChangeRegistry", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly Session session;
    private readonly Stack<TrackingStackFrame> stack;

    private void Subscribe()
    {
      session.Events.Persisting += OnPersisting;
      session.Events.TransactionOpened += OnOpenTransaction;
      session.Events.TransactionCommitted += OnCommitTransaction;
      session.Events.TransactionRollbacked += OnRollbackTransaction;
    }

    private void OnOpenTransaction(object sender, TransactionEventArgs e)
    {
      stack.Push(new TrackingStackFrame());
    }

    private void OnCommitTransaction(object sender, TransactionEventArgs e)
    {
      var source = stack.Pop();
      var target = stack.Peek();
      target.MergeWith(source);

      if (e.Transaction.IsNested)
        return;

      var items = target.Cast<ITrackingItem>().ToList().AsReadOnly();
      target.Clear();

      RaiseTrackingCompleted(new TrackingCompletedEventArgs(session, items));
    }

    private void OnRollbackTransaction(object sender, TransactionEventArgs e)
    {
      stack.Pop();
    }

    private void OnPersisting(object sender, EventArgs e)
    {
      var accessor = (SessionEventAccessor) sender;
      var registry = GetEntityChangeRegistry(session);
      if (registry.Count==0)
        return;

      var frame = stack.Peek();

      foreach (var state in registry.GetItems(PersistenceState.Removed))
        frame.Register(new TrackingItem(state.Key, TrackingItemState.Deleted, state.DifferentialTuple));

      foreach (var state in registry.GetItems(PersistenceState.New))
        frame.Register(new TrackingItem(state.Key, TrackingItemState.Created, state.DifferentialTuple));

      foreach (var state in registry.GetItems(PersistenceState.Modified))
        frame.Register(new TrackingItem(state.Key, TrackingItemState.Changed, state.DifferentialTuple));
    }

    private static EntityChangeRegistry GetEntityChangeRegistry(Session session)
    {
      return (EntityChangeRegistry) RegistryAccessor.GetValue(session, null);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionTrackingMonitor"/> class.
    /// </summary>
    /// <param name="session"><see cref="T:Xtensive.Orm.Session"/>, to which current instance
    /// is bound.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    [ServiceConstructor]
    public SessionTrackingMonitor(Session session)
    {
      if (session==null)
        throw new ArgumentNullException("session");
      this.session = session;
      stack = new Stack<TrackingStackFrame>();
      stack.Push(new TrackingStackFrame());
      Subscribe();
    }
  }
}