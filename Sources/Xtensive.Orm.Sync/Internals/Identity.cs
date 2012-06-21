using System;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Pair of <see cref="Key"/> and <see cref="Guid"/>.
  /// </summary>
  public class Identity
  {
    /// <summary>
    /// Gets or sets the global id.
    /// </summary>
    /// <value>
    /// The global id.
    /// </value>
    public Guid GlobalId { get; set;}

    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    /// <value>
    /// The key.
    /// </value>
    public Key Key { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Identity"/> class.
    /// </summary>
    /// <param name="key">The key.</param>
    public Identity(Key key)
    {
      Key = key;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Identity"/> class.
    /// </summary>
    /// <param name="globalId">The global id.</param>
    /// <param name="key">The key.</param>
    public Identity(Guid globalId, Key key)
    {
      GlobalId = globalId;
      Key = key;
    }
  }
}
