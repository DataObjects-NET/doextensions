using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Configuration for <see cref="OrmSyncProvider"/>
  /// </summary>
  public sealed class SyncConfiguration
  {
    /// <summary>
    /// Gets or sets a value indicating whether all instances of all types should be synchronized.
    /// </summary>
    /// <value>
    ///   <c>true</c> if all instances of all types should be synchronized; otherwise, <c>false</c>.
    /// </value>
    public bool SyncAll { get; set; }

    /// <summary>
    /// Gets the types that should be synchronized.
    /// </summary>
    public HashSet<Type> SyncTypes { get; private set; }

    /// <summary>
    /// Gets the types that should not be synchronized.
    /// </summary>
    public HashSet<Type> SkipTypes { get; private set; }

    /// <summary>
    /// Gets filters that should be applied to instances of types that should be synchronized.
    /// </summary>
    public Dictionary<Type, Expression> Filters { get; private set; }

    /// <summary>
    /// Gets or sets the size of the synchronization batch.
    /// </summary>
    public int BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Session"/> to use for synchronization.
    /// If set to null separate session would be created.
    /// </summary>
    public Session Session { get; set; }

    /// <summary>
    /// Resets this instance.
    /// </summary>
    public void Reset()
    {
      Initialize();
    }

    internal void Prepare()
    {
      if (SyncTypes.Count==0 && Filters.Count==0 && SkipTypes.Count==0)
        SyncAll = true;
    }

    private void Initialize()
    {
      SyncAll = false;
      BatchSize = WellKnown.SyncBatchSize;
      SyncTypes = new HashSet<Type>();
      SkipTypes = new HashSet<Type>();
      Filters = new Dictionary<Type, Expression>();
    }

    internal SyncConfiguration()
    {
      Initialize();
    }
  }
}
