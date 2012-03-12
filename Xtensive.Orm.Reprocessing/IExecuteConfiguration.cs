using System;
using System.Transactions;

namespace Xtensive.Orm.Reprocessing
{
  public interface IExecuteConfiguration
  {
    IExecuteConfiguration WithStrategy(IExecuteActionStrategy strategy);
    IExecuteConfiguration WithIsolationLevel(IsolationLevel isolationLevel);
    IExecuteConfiguration WithTransactionOpenMode(TransactionOpenMode transactionOpenMode);
    void Execute(Action<Session> action);
    T Execute<T>(Func<Session, T> func);
  }
}
