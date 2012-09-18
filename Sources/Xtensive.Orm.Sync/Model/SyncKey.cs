using System;
using Xtensive.Tuples;

namespace Xtensive.Orm.Sync.Model
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
    public int Type { get; private set; }

    /// <summary>
    /// Gets key value of the <see cref="Entity"/>.
    /// </summary>
    [Field]
    public string Value { get; private set; }

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
      if (key.TypeReference.Accuracy!=TypeReferenceAccuracy.ExactType)
        throw new ArgumentException("Key does not have exact type", "key");

      this.key = key;

      Type = key.TypeInfo.TypeId;
      Value = key.Value.Format();
    }
  }
}