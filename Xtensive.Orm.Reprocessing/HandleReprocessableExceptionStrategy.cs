namespace Xtensive.Orm.Reprocessing
{
  /// <summary>
  /// Reprocess task when <see cref="ReprocessableException"/> is thrown.
  /// </summary>
  public class HandleReprocessableExceptionStrategy : ExecuteActionStrategy
  {
    #region Non-public methods

    protected override bool HandleException(ExecuteErrorEventArgs eventArgs)
    {
      if (eventArgs.Exception is ReprocessableException)
        return OnError(eventArgs);
      return base.HandleException(eventArgs);
    }

    #endregion
  }
}