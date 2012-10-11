using System;
using System.Collections.Generic;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync.DataExchange
{
  /// <summary>
  /// Additional information about and <see cref="Entity"/> being synchronized
  /// </summary>
  [Serializable]
  public sealed class ItemChangeData
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
    /// Gets or sets the tuple value in canonical representation.
    /// </summary>
    public string TupleValue { get; set; }

    /// <summary>
    /// Gets the references from this entity.
    /// </summary>
    public Dictionary<string, Identity> References { get; private set; }

    /// <summary>
    /// Binds all internal structures to <see cref="Domain"/>.
    /// </summary>
    /// <param name="domain">The domain.</param>
    public void BindTo(Domain domain)
    {
      Identity.BindTo(domain);

      foreach (var identity in References.Values)
        identity.BindTo(domain);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemChangeData"/> class.
    /// </summary>
    public ItemChangeData()
    {
      References = new Dictionary<string,Identity>();
    }
  }
}
