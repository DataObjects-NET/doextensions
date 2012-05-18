using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xtensive.IoC;
using Xtensive.Orm.Internals;

namespace Xtensive.Orm.Tracking
{
  [Service(typeof (SessionTrackingMonitor), Singleton = true)]
  public class SessionTrackingMonitor : SessionBound, ISessionTrackingMonitor
  {
    private static readonly PropertyInfo registryAccessor = typeof (Session).GetProperty("EntityChangeRegistry", BindingFlags.Instance | BindingFlags.NonPublic);

    private bool isDisposed;
    private bool isRunning;
    private Action<TrackingResult> callbackAction;

    public bool IsRunning { get { return isRunning; } }

    public void Start(Action<TrackingResult> callback)
    {
      if (isRunning)
        return;

      callbackAction = callback;
      var stack = new TrackingStack();
      stack.Push(new TrackingStackFrame());
      Session.Extensions.Set(stack);
      Attach();
    }

    public void Stop()
    {
      isRunning = false;
      Detach();
      Session.Extensions.Set<TrackingStack>(null);
      callbackAction = null;
    }

    private void Attach()
    {
      Session.Events.Persisting += OnPersisting;
      Session.Events.TransactionOpened += OnOpenTransaction;
      Session.Events.TransactionCommitted += OnCommitTransaction;
      Session.Events.TransactionRollbacked += OnRollBackTransaction;
      Session.Events.Disposing += OnDisposeSession;
    }

    private void Detach()
    {
      Session.Events.Persisting -= OnPersisting;
      Session.Events.TransactionOpened -= OnOpenTransaction;
      Session.Events.TransactionCommitted -= OnCommitTransaction;
      Session.Events.TransactionRollbacked -= OnRollBackTransaction;
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

      var items = target.ToList() as IEnumerable<ITrackingItem>;
      target.Clear();
      callbackAction.Invoke(new TrackingResult(items));
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

    private void OnDisposeSession(object sender, EventArgs e)
    {
      Dispose();
    }

    private static EntityChangeRegistry GetEntityChangeRegistry(Session session)
    {
      return (EntityChangeRegistry) registryAccessor.GetValue(session, null);
    }

    void IDisposable.Dispose()
    {
      if (isDisposed)
        return;

      Dispose();
    }

    private void Dispose()
    {
      if (Session==null)
        return;
      if (Session.Events==null)
        return;

      try {
        Stop();
      }
      finally {
        isDisposed = true;
      }
    }

    [ServiceConstructor]
    public SessionTrackingMonitor(Session session)
      : base(session)
    {
    }
  }
}