using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Configuration for <see cref="OrmSyncProvider"/>
  /// </summary>
  public class SyncConfiguration
  {
    /// <summary>
    /// Gets the <see cref="SyncConfigurationEndpoint"/> instance for fluent configuration.
    /// </summary>
    public SyncConfigurationEndpoint Endpoint { get; private set; }

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
    /// Resets this instance.
    /// </summary>
    public void Reset()
    {
      Initialize();
    }

    private void Initialize()
    {
      BatchSize = Wellknown.SyncBatchSize;
      Endpoint = new SyncConfigurationEndpoint(this);
      SyncTypes = new HashSet<Type>();
      SkipTypes = new HashSet<Type>();
      Filters = new Dictionary<Type, Expression>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncConfiguration"/> class.
    /// </summary>
    public SyncConfiguration()
    {
      Initialize();
    }
  }
}
