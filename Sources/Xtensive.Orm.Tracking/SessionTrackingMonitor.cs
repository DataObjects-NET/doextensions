using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xtensive.IoC;
using Xtensive.Orm.Internals;

namespace Xtensive.Orm.Tracking
{
  /// <summary>
  /// Implementation of <see cref="ISessionTrackingMonitor"/> interface.
  /// </summary>
  [Service(typeof (ISessionTrackingMonitor), Singleton = true)]
  public class SessionTrackingMonitor : SessionBound, ISessionTrackingMonitor
  {
    private static readonly PropertyInfo registryAccessor = typeof (Session).GetProperty("EntityChangeRegistry", BindingFlags.Instance | BindingFlags.NonPublic);

    private int subscriberNumber;
    private bool isDisposed;
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
        lock (gate)
          trackingCompletedHandler -= value;
        subscriberNumber--;
        if (!HasSubscribers)
          Detach();
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
      var stack = new TrackingStack();
      stack.Push(new TrackingStackFrame());
      Session.Extensions.Set(stack);
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
      Session.Extensions.Set<TrackingStack>(null);
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

      var items = target.Cast<ITrackingItem>().ToList();
      target.Clear();
      trackingCompletedHandler.Invoke(this, new TrackingCompletedEventArgs(items));
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

      foreach (var state in GetItems(registry, PersistenceState.Removed))
        frame.Register(new TrackingItem(state.Key, state.DifferentialTuple, TrackingItemState.Deleted));

      foreach (var state in GetItems(registry, PersistenceState.New))
        frame.Register(new TrackingItem(state.Key, state.DifferentialTuple, TrackingItemState.Created));

      foreach (var state in GetItems(registry, PersistenceState.Modified))
        frame.Register(new TrackingItem(state.Key, state.DifferentialTuple, TrackingItemState.Changed));
    }

    private IEnumerable<EntityState> GetItems(EntityChangeRegistry registry, PersistenceState state)
    {
      var result = registry.GetItems(state);
      if (Filter!=null)
        result = result.Where(i => Filter(i.Type.UnderlyingType));
      return result;
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
      : base(session)
    {
    }
  }
}