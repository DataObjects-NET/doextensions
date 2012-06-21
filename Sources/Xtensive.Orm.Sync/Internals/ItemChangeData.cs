using System;
using System.Collections.Generic;
using Microsoft.Synchronization;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Additional information about and <see cref="Entity"/> being synchronized
  /// </summary>
  [Serializable]
  public class ItemChangeData
  {
    /// <summary>
    /// Gets or sets the change.
    /// </summary>
    /// <value>
    /// The change.
    /// </value>
    public ItemChange Change { get; set; }

    /// <summary>
    /// Gets or sets the identity.
    /// </summary>
    /// <value>
    /// The identity.
    /// </value>
    public Identity Identity { get; set; }

    /// <summary>
    /// Gets or sets the tuple.
    /// </summary>
    /// <value>
    /// The tuple.
    /// </value>
    public Tuple Tuple { get; set; }

    /// <summary>
    /// Gets the references from this Entity .
    /// </summary>
    public Dictionary<string, Identity> References { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemChangeData"/> class.
    /// </summary>
    public ItemChangeData()
    {
      References = new Dictionary<string,Identity>();
    }
  }
}
