using System;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Defines unique identifier for persistent type hierarchy.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
  public class HierarchyIdAttribute : Attribute
  {
    /// <summary>
    /// Gets hierarchy identifier.
    /// </summary>
    public int HierarchyId { get; private set; }

    /// <summary>
    /// Creates new instance of <see cref="HierarchyIdAttribute"/> class.
    /// </summary>
    /// <param name="hierarchyId">Hierarchy identifier.</param>
    public HierarchyIdAttribute(int hierarchyId)
    {
      HierarchyId = hierarchyId;
    }
  }
}