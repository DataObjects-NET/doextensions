using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Synchronization;
using Xtensive.Tuples;
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

    [NonSerialized]
    private Tuple tuple;

    private string tupleValue;

    /// <summary>
    /// Gets or sets the tuple.
    /// </summary>
    public Tuple Tuple
    {
      get { return tuple; }
      set { tuple = value; }
    }

    [OnSerializing]
    private void OnSerializing(StreamingContext context)
    {
      if (tuple==null)
        return;

      tupleValue = tuple.Format();
    }

    /// <summary>
    /// Gets the references from this Entity .
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

      if (!string.IsNullOrEmpty(tupleValue))
        tuple = Tuple.Parse(Identity.Key.TypeReference.Type.TupleDescriptor, tupleValue);
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
