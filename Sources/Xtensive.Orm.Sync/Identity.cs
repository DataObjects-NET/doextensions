using System;
using System.Runtime.Serialization;
using Xtensive.Core;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Pair of <see cref="Key"/> and <see cref="Guid"/>.
  /// </summary>
  [Serializable]
  public sealed class Identity
  {
    [NonSerialized]
    private Key key;

    private string keyValue;

    /// <summary>
    /// Gets or sets the global id.
    /// </summary>
    public Guid GlobalId { get; set;}

    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    public Key Key
    {
      get { return key; }
      set
      {
        ArgumentValidator.EnsureArgumentNotNull(value, "value");
        key = value;
      }
    }

    [OnSerializing]
    private void OnSerializing(StreamingContext context)
    {
      keyValue = key.Format();
    }

    /// <summary>
    /// Binds all internal structures to <see cref="Domain"/>.
    /// </summary>
    /// <param name="domain">The domain.</param>
    public void BindTo(Domain domain)
    {
      Key = Key.Parse(domain, keyValue);
    }

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
    /// <param name="key">The key.</param>
    /// <param name="globalId">The global id.</param>
    public Identity(Key key, Guid globalId)
    {
      Key = key;
      GlobalId = globalId;
    }
  }
}
