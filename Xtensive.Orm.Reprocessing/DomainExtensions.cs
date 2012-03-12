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
    /// <param name="isolationLevel">Isolation level of the task.</param>
    /// <param name="transactionOpenMode">Transaction open mode of the task.</param>
    /// <param name="strategy">Execute strategy of the task.</param>
    /// <param name="action">Task with <see cref="Void"/> result.</param>
    public static void Execute(
      this Domain domain,
      Action<Session> action,
      IExecuteActionStrategy strategy = null,
      IsolationLevel isolationLevel = IsolationLevel.Unspecified,
      TransactionOpenMode? transactionOpenMode = null)
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
      IExecuteActionStrategy strategy = null,
      IsolationLevel isolationLevel = IsolationLevel.Unspecified,
      TransactionOpenMode? transactionOpenMode = null)
    {
      return ExecuteInternal(domain, isolationLevel, transactionOpenMode, strategy, func);
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

    public static IExecuteConfiguration WithIsolationLevel(this Domain domain, IsolationLevel isolationLevel)
    {
      return new ExecuteConfiguration(domain).WithIsolationLevel(isolationLevel);
    }

    public static IExecuteConfiguration WithStrategy(this Domain domain, IExecuteActionStrategy strategy)
    {
      return new ExecuteConfiguration(domain).WithStrategy(strategy);
    }

    public static IExecuteConfiguration WithTransactionOpenMode(this Domain domain, TransactionOpenMode transactionOpenMode)
    {
      return new ExecuteConfiguration(domain).WithTransactionOpenMode(transactionOpenMode);
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