﻿namespace Xtensive.Orm.Reprocessing
{
  /// <summary>
  /// Contains standard reprocess strategies.
  /// </summary>
  public enum StandardExecutionStrategy
  {
    /// <summary>
    /// <see cref="HandleReprocessableExceptionStrategy"/>
    /// </summary>
    Reprocessable,
    /// <summary>
    /// <see cref="NoReprocessStrategy"/>
    /// </summary>
    NoReprocess,
    /// <summary>
    /// <see cref="HandleUniqueConstraintViolationStrategy"/>
    /// </summary>
    UniqueConstraintViolation
  }
}
