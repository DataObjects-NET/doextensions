using System;
using System.Linq;
using System.Reflection;
using Xtensive.IoC;
using Xtensive.Orm.Internals;

namespace Xtensive.Orm.Tracking
{
  [Service(typeof (ISessionTrackingMonitor), Singleton = true)]
  internal sealed class SessionTrackingMonitor : ISessionTrackingMonitor, ISessionService, IDisposable
  {
    private static readonly PropertyInfo RegistryAccessor = typeof (Session).GetProperty("EntityChangeRegistry", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly object gate = new object();

    private readonly Session session;
    private int subscriberNumber;

    private bool isDisabled;
    private bool isDisposed;

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
        lock (gate)
          TrackingCompletedHandler -= value;
        subscriberNumber--;
        if (!HasSubscribers)
          Detach();
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
      var stack = new TrackingStack();
      stack.Push(new TrackingStackFrame());
      session.Extensions.Set(stack);
      session.Events.Persisting += OnPersisting;
      session.Events.TransactionOpened += OnOpenTransaction;
      session.Events.TransactionCommitted += OnCommitTransaction;
      session.Events.TransactionRollbacked += OnRollBackTransaction;
      session.Events.Disposing += OnDisposeSession;
    }

    private void Detach()
    {
      session.Events.Persisting -= OnPersisting;
      session.Events.TransactionOpened -= OnOpenTransaction;
      session.Events.TransactionCommitted -= OnCommitTransaction;
      session.Events.TransactionRollbacked -= OnRollBackTransaction;
      session.Extensions.Set<TrackingStack>(null);
    }

    private void OnOpenTransaction(object sender, TransactionEventArgs e)
    {
      var stack = session.Extensions.Get<TrackingStack>();
      if (stack==null)
        return;

      stack.Push(new TrackingStackFrame());
    }

    private void OnCommitTransaction(object sender, TransactionEventArgs e)
    {
      var stack = session.Extensions.Get<TrackingStack>();
      if (stack==null)
        return;

      var source = stack.Pop();
      var target = stack.Peek();
      target.MergeWith(source);

      if (e.Transaction.IsNested)
        return;

      var items = target.Cast<ITrackingItem>().ToList();
      target.Clear();
      TrackingCompletedHandler.Invoke(this, new TrackingCompletedEventArgs(session, items));
    }

    private void OnRollBackTransaction(object sender, TransactionEventArgs e)
    {
      var stack = session.Extensions.Get<TrackingStack>();
      if (stack==null)
        return;
      stack.Pop();
    }

    private void OnPersisting(object sender, EventArgs e)
    {
      var accessor = (SessionEventAccessor) sender;
      var registry = GetEntityChangeRegistry(session);
      if (registry.Count==0)
        return;

      var stack = session.Extensions.Get<TrackingStack>();
      if (stack==null)
        return;
      var frame = stack.Peek();

      foreach (var state in registry.GetItems(PersistenceState.Removed))
        frame.Register(new TrackingItem(state.Key, state.DifferentialTuple, TrackingItemState.Deleted));

      foreach (var state in registry.GetItems(PersistenceState.New))
        frame.Register(new TrackingItem(state.Key, state.DifferentialTuple, TrackingItemState.Created));

      foreach (var state in registry.GetItems(PersistenceState.Modified))
        frame.Register(new TrackingItem(state.Key, state.DifferentialTuple, TrackingItemState.Changed));
    }

    private void OnDisposeSession(object sender, EventArgs e)
    {
      Dispose();
    }

    private static EntityChangeRegistry GetEntityChangeRegistry(Session session)
    {
      return (EntityChangeRegistry) RegistryAccessor.GetValue(session, null);
    }

    void IDisposable.Dispose()
    {
      if (isDisposed)
        return;

      Dispose();
    }

    private void Dispose()
    {
      if (session==null)
        return;
      if (session.Events==null)
        return;

      try {
        Detach();
      }
      finally {
        isDisposed = true;
      }
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
    }
  }
}