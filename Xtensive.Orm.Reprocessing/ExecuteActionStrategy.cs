using System;
using System.Collections.Concurrent;
using System.Transactions;
using Xtensive.Core;
using Xtensive.Orm.Configuration;
using SystemTransaction = System.Transactions.Transaction;

namespace Xtensive.Orm.Reprocessing
{
  /// <summary>
  /// Base class for the standard execute action strategies.
  /// </summary>
  public abstract class ExecuteActionStrategy : IExecuteActionStrategy
  {
    private static readonly ConcurrentDictionary<Type, IExecuteActionStrategy> Singletons =
      new ConcurrentDictionary<Type, IExecuteActionStrategy>();

    /// <summary>
    /// Gets singleton of the <see cref="HandleReprocessableExceptionStrategy"/>.
    /// </summary>
    public static readonly HandleReprocessableExceptionStrategy Reprocessable =
      new HandleReprocessableExceptionStrategy();

    /// <summary>
    /// Gets singleton of the <see cref="HandleUniqueConstraintViolationStrategy"/>.
    /// </summary>
    public static readonly HandleUniqueConstraintViolationStrategy UniqueConstraintViolation =
      new HandleUniqueConstraintViolationStrategy();

    /// <summary>
    /// Gets singleton of the <see cref="NoReprocessStrategy"/>.
    /// </summary>
    public static readonly NoReprocessStrategy NoReprocess = new NoReprocessStrategy();

    /// <summary>
    /// Gets or sets attempts limit.
    /// </summary>
    public int Attempts { get; set; }

    public virtual T Execute<T>(ExecutionContext<T> context)
    {
      int i = 0;
      while (true) {
        Session session = null;
        TransactionScope tran = null;
        bool needBreak = false;
        Transaction transaction = null;
        try {
          T result;
          SessionScope sessionScope = null;
          try {
            try {
              IsolationLevel isolationLevel = context.IsolationLevel;
              SystemTransaction currentTransaction = SystemTransaction.Current;
              if (isolationLevel==IsolationLevel.Unspecified && currentTransaction!=null)
                isolationLevel = currentTransaction.IsolationLevel;
              var sessionConfig = new SessionConfiguration {DefaultIsolationLevel = isolationLevel};
              session = SessionScope.CurrentSession;
              if (session==null) {
                session = context.Domain.OpenSession(sessionConfig);
                sessionScope = session.Activate();
              }
              if (currentTransaction!=null && session.Transaction!=null)
                session.EnsureTransactionIsStarted();
              tran = currentTransaction!=null &&
                currentTransaction.TransactionInformation.DistributedIdentifier!=Guid.Empty
                ? session.OpenTransaction(isolationLevel)
                : session.OpenTransaction(context.TransactionOpenMode, isolationLevel);
              transaction = session.Transaction;
              result = context.Function(session);
              tran.Complete();
            }
            finally {
              try {
                tran.DisposeSafely();
              }
              catch (StorageException e) {
                if (e.InnerException==null || !(e.InnerException is InvalidOperationException) ||
                  e.InnerException.Source!="System.Data")
                  throw;
                if (tran.Transaction.IsNested)
                  needBreak = true;
              }
            }
          }
          finally {
            sessionScope.DisposeSafely();
            if (SessionScope.CurrentSession!=session)
              session.DisposeSafely();
          }
          return result;
        }
        catch (Exception e) {
          if ((SystemTransaction.Current!=null &&
            SystemTransaction.Current.TransactionInformation.DistributedIdentifier!=Guid.Empty) ||
              (transaction!=null && transaction.State==TransactionState.Active))
            throw;
          if (e is RollbackTransactionException)
            return default(T);
          i++;
          if (needBreak || !HandleException(new ExecuteErrorEventArgs(e, session, transaction, i)))
            throw;
        }
      }
    }

    /// <summary>
    /// Gets the singleton.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetSingleton<T>()
    {
      return (T) GetSingleton(typeof (T));
    }

    /// <summary>
    /// Gets the singleton of the strategy
    /// </summary>
    /// <param name="type">The type of the strategy.</param>
    /// <returns>The singleton.</returns>
    public static IExecuteActionStrategy GetSingleton(Type type)
    {
      if (type==typeof (HandleReprocessableExceptionStrategy))
        return Reprocessable;
      if (type==typeof (HandleUniqueConstraintViolationStrategy))
        return UniqueConstraintViolation;
      if (type==typeof (NoReprocessStrategy))
        return NoReprocess;
      return Singletons.GetOrAdd(type, a => (IExecuteActionStrategy) Activator.CreateInstance(type));
    }

    #region Non-public methods

    protected virtual bool HandleException(ExecuteErrorEventArgs eventArgs)
    {
      return false;
    }

    protected virtual bool OnError(ExecuteErrorEventArgs eventArgs)
    {
      if (Error!=null)
        Error(this, eventArgs);
      return eventArgs.Attempt < Attempts;
    }

    #endregion

    #region IExecuteActionStrategy Members

    /// <summary>
    /// Occurs when unhandled exception is thrown
    /// </summary>
    public event EventHandler<ExecuteErrorEventArgs> Error;

    #endregion

    protected ExecuteActionStrategy()
    {
      Attempts = 5;
    }
  }
}