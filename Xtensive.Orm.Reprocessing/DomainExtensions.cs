using System;
using System.Transactions;
using Xtensive.Orm.Reprocessing.Configuration;

namespace Xtensive.Orm.Reprocessing
{
  /// <summary>
  /// Extends <see cref="Domain"/>.
  /// </summary>
  public static class DomainExtensions
  {
    /// <summary>
    /// Executes a reprocessable task.
    /// </summary>
    /// <param name="domain">The domain of the task.</param>
    /// <param name="action">The task with <see cref="Void"/> result.</param>
    public static void Execute(this Domain domain, Action<Session> action)
    {
      ExecuteInternal(domain, IsolationLevel.Unspecified, null, null, action);
    }

    /// <summary>
    /// Executes a reprocessable task.
    /// </summary>
    /// <param name="domain">The domain of the task.</param>
    /// <param name="func">The task with T result.</param>
    /// <typeparam name="T">Return type of the task.</typeparam>
    /// <returns>The task result.</returns>
    public static T Execute<T>(this Domain domain, Func<Session, T> func)
    {
      return ExecuteInternal(domain, IsolationLevel.Unspecified, null, null, func);
    }

    /// <summary>
    /// Executes a reprocessable task.
    /// </summary>
    /// <param name="domain">The domain of the task.</param>
    /// <param name="isolationLevel">Isolation level of the task.</param>
    /// <param name="action">The task with <see cref="Void"/> result.</param>
    public static void Execute(this Domain domain, Action<Session> action, IsolationLevel isolationLevel)
    {
      ExecuteInternal(domain, isolationLevel, null, null, action);
    }

    /// <summary>
    /// Executes a reprocessable task.
    /// </summary>
    /// <param name="domain">The domain of the task.</param>
    /// <param name="strategy">Execute strategy of the task.</param>
    /// <param name="func">The task with T result.</param>
    /// <typeparam name="T">Return type of the task.</typeparam>
    /// <returns>The task result.</returns>
    public static T Execute<T>(this Domain domain, Func<Session, T> func, IExecuteActionStrategy strategy)
    {
      return ExecuteInternal(domain, IsolationLevel.Unspecified, null, strategy, func);
    }

    /// <summary>
    /// Executes a reprocessable task.
    /// </summary>
    /// <param name="domain">The domain of the task.</param>
    /// <param name="strategy">Execute strategy of the task.</param>
    /// <param name="action">The task with <see cref="Void"/> result.</param>
    public static void Execute(this Domain domain, Action<Session> action, IExecuteActionStrategy strategy)
    {
      ExecuteInternal(domain, IsolationLevel.Unspecified, null, strategy, action);
    }


    /// <summary>
    /// Executes a reprocessable task.
    /// </summary>
    /// <param name="domain">The domain of the task.</param>
    /// <param name="isolationLevel">Isolation level of the task.</param>
    /// <param name="func">Task with T result.</param>
    /// <typeparam name="T">Return type of the task.</typeparam>
    /// <returns>The task result.</returns>
    public static T Execute<T>(this Domain domain, Func<Session, T> func, IsolationLevel isolationLevel)
    {
      return ExecuteInternal(domain, isolationLevel, null, null, func);
    }

    /// <summary>
    /// Executes a reprocessable task.
    /// </summary>
    /// <param name="domain">The domain of the task.</param>
    /// <param name="isolationLevel">Isolation level of the task.</param>
    /// <param name="strategy">Execute strategy of the task.</param>
    /// <param name="action">The task with <see cref="Void"/> result.</param>
    public static void Execute(
      this Domain domain, Action<Session> action, IsolationLevel isolationLevel, IExecuteActionStrategy strategy)
    {
      ExecuteInternal(domain, isolationLevel, null, strategy, action);
    }

    /// <summary>
    /// Executes a reprocessable task.
    /// </summary>
    /// <param name="domain">The domain.</param>
    /// <param name="isolationLevel">Isolation level of the task.</param>
    /// <param name="strategy">Execute strategy of the task.</param>
    /// <param name="func">The task with T result.</param>
    /// <typeparam name="T">Return type of the task.</typeparam>
    /// <returns>The task result.</returns>
    public static T Execute<T>(
      this Domain domain, Func<Session, T> func, IsolationLevel isolationLevel, IExecuteActionStrategy strategy)
    {
      return ExecuteInternal(domain, isolationLevel, null, strategy, func);
    }

    /// <summary>
    /// Executes a reprocessable task.
    /// </summary>
    /// <param name="domain">The domain of the task.</param>
    /// <param name="isolationLevel">Isolation level of the task.</param>
    /// <param name="transactionOpenMode">Transaction open mode of the task.</param>
    /// <param name="strategy">Execute strategy of the task.</param>
    /// <param name="action">Task with <see cref="Void"/> result.</param>
    public static void Execute(
      this Domain domain,
      Action<Session> action,
      IsolationLevel isolationLevel,
      TransactionOpenMode transactionOpenMode,
      IExecuteActionStrategy strategy)
    {
      ExecuteInternal(domain, isolationLevel, transactionOpenMode, strategy, action);
    }

    /// <summary>
    /// Executes a reprocessable task.
    /// </summary>
    /// <param name="domain">The domain of the task.</param>
    /// <param name="isolationLevel">Isolation level of the task.</param>
    /// <param name="transactionOpenMode">Transaction open mode of the task.</param>
    /// <param name="strategy">Execute strategy of the task.</param>
    /// <param name="func">Task with T result.</param>
    /// <typeparam name="T">Return type of the task.</typeparam>
    /// <returns>The task result.</returns>
    public static T Execute<T>(
      this Domain domain,
      Func<Session, T> func,
      IsolationLevel isolationLevel,
      TransactionOpenMode transactionOpenMode,
      IExecuteActionStrategy strategy)
    {
      return ExecuteInternal(domain, isolationLevel, transactionOpenMode, strategy, func);
    }

    /// <summary>
    /// Executes a reprocessable task.
    /// </summary>
    /// <param name="domain">The domain of the task.</param>
    /// <param name="isolationLevel">Isolation level of the task.</param>
    /// <param name="transactionOpenMode">Transaction open mode of the task.</param>
    /// <param name="action">The task with <see cref="Void"/> result.</param>
    public static void Execute(
      this Domain domain, Action<Session> action, IsolationLevel isolationLevel, TransactionOpenMode transactionOpenMode)
    {
      ExecuteInternal(domain, isolationLevel, transactionOpenMode, null, action);
    }

    /// <summary>
    /// Executes a reprocessable task.
    /// </summary>
    /// <param name="domain">The domain of the task.</param>
    /// <param name="isolationLevel">Isolation level of the task.</param>
    /// <param name="transactionOpenMode">Transaction open mode of the task.</param>
    /// <param name="func">The task with T result.</param>
    /// <typeparam name="T">Return type of the task.</typeparam>
    /// <returns>The task result.</returns>
    public static T Execute<T>(
      this Domain domain, Func<Session, T> func, IsolationLevel isolationLevel, TransactionOpenMode transactionOpenMode)
    {
      return ExecuteInternal(domain, isolationLevel, transactionOpenMode, null, func);
    }

    /// <summary>
    /// Executes a reprocessable task.
    /// </summary>
    /// <param name="domain">The domain of the task.</param>
    /// <param name="transactionOpenMode">Transaction open mode of the task.</param>
    /// <param name="action">The task with <see cref="Void"/> result.</param>
    public static void Execute(this Domain domain, Action<Session> action, TransactionOpenMode transactionOpenMode)
    {
      ExecuteInternal(domain, IsolationLevel.Unspecified, transactionOpenMode, null, action);
    }

    /// <summary>
    /// Executes a reprocessable task.
    /// </summary>
    /// <param name="domain">The domain of the task.</param>
    /// <param name="transactionOpenMode">Transaction open mode of the task.</param>
    /// <param name="func">The task with T result.</param>
    /// <typeparam name="T">Return type of the task.</typeparam>
    /// <returns>The task result.</returns>
    public static T Execute<T>(this Domain domain, Func<Session, T> func, TransactionOpenMode transactionOpenMode)
    {
      return ExecuteInternal(domain, IsolationLevel.Unspecified, transactionOpenMode, null, func);
    }

    /// <summary>
    /// Gets the reprocessing configuration.
    /// </summary>
    /// <param name="domain">The domain.</param>
    /// <returns>The reprocessing configuration.</returns>
    public static ReprocessingConfiguration GetReprocessingConfiguration(this Domain domain)
    {
      var result = domain.Extensions.Get<ReprocessingConfiguration>();
      if (result==null) {
        result = ReprocessingConfiguration.Load();
        domain.Extensions.Set(result);
      }
      return result;
    }

    #region Non-public methods

    internal static void ExecuteInternal(
      this Domain domain,
      IsolationLevel isolationLevel,
      TransactionOpenMode? transactionOpenMode,
      IExecuteActionStrategy strategy,
      Action<Session> action)
    {
      ExecuteInternal<object>(
        domain,
        isolationLevel,
        transactionOpenMode,
        strategy,
        a => {
          action(a);
          return null;
        });
    }


    internal static T ExecuteInternal<T>(
      this Domain domain,
      IsolationLevel isolationLevel,
      TransactionOpenMode? transactionOpenMode,
      IExecuteActionStrategy strategy,
      Func<Session, T> func)
    {
      ReprocessingConfiguration config = domain.GetReprocessingConfiguration();
      if (strategy==null)
        strategy = ExecuteActionStrategy.GetSingleton(config.DefaultExecuteStrategy);
      if (transactionOpenMode==null)
        transactionOpenMode = config.DefaultTransactionOpenMode;
      return strategy.Execute(new ExecutionContext<T>(domain, isolationLevel, transactionOpenMode.Value, func));
    }

    #endregion
  }
}