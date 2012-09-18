using System;
using Xtensive.Tuples;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Persistent storage for <see cref="Orm.Key"/>.
  /// </summary>
  public sealed class SyncKey : Structure
  {
    private Key key;

    /// <summary>
    /// Gets type of the <see cref="Entity"/>.
    /// </summary>
    [Field]
    private int Type { get; set; }

    /// <summary>
    /// Gets key value of the <see cref="Entity"/>.
    /// </summary>
    [Field]
    private string Value { get; set; }

    /// <summary>
    /// Gets <see cref="Entity"/> key.
    /// </summary>
    public Key Key
    {
      get
      {
        if (key!=null)
          return key;
        if (Type <= 0 || Value==null)
          return null;
        return key = ParseKey();
      }
    }

    private Key ParseKey()
    {
      var domain = Session.Domain;
      var type = domain.Model.Types[Type];
      var keyTuple = type.Key.TupleDescriptor.Parse(Value);
      return Key.Create(domain, type.UnderlyingType, TypeReferenceAccuracy.ExactType, keyTuple);
    }

    /// <summary>
    /// Creates new instance of <see cref="SyncKey"/> type.
    /// </summary>
    /// <param name="session">Session to use.</param>
    /// <param name="key"><see cref="Entity"/> key.</param>
    public SyncKey(Session session, Key key)
      : base(session)
    {
      if (key==null)
        throw new ArgumentNullException("key");
      if (key.TypeInfo==null)
        throw new ArgumentException("Key does not have exact type", "key");

      this.key = key;

      Type = key.TypeInfo.TypeId;
      Value = key.Value.Format();
    }
  }
}