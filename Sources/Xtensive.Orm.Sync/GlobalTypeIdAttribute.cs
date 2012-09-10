using System;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Defines global type identifier for persistent type.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
  public class GlobalTypeIdAttribute : Attribute
  {
    /// <summary>
    /// Gets type hash.
    /// </summary>
    public int GlobalTypeId { get; private set; }

    /// <summary>
    /// Creates new instance of <see cref="GlobalTypeIdAttribute"/> class.
    /// </summary>
    /// <param name="globalTypeId">Type global id.</param>
    public GlobalTypeIdAttribute(int globalTypeId)
    {
      GlobalTypeId = globalTypeId;
    }
  }
}