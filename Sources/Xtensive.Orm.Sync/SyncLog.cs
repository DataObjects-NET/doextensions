using System;
using Xtensive.Orm.Tracking;
using Xtensive.Tuples;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Log entry with information about changed entity.
  /// </summary>
  [HierarchyRoot(Clustered = false), KeyGenerator(Name = WellKnown.TickGeneratorName)]
  public sealed class SyncLog : Entity
  {
    private Key cachedEntityKey;

    /// <summary>
    /// Gets tick when change occured.
    /// This field is a primary key.
    /// </summary>
    [Key, Field]
    public long Tick { get; private set; }

    /// <summary>
    /// Gets type id of entity that was changed.
    /// </summary>
    [Field]
    public int EntityTypeId { get; private set; }

    /// <summary>
    /// Gets key tuple of entity that was changed.
    /// </summary>
    [Field]
    public string EntityKeyTuple { get; private set; }

    /// <summary>
    /// Gets key of entity that was changed.
    /// </summary>
    public Key EntityKey
    {
      get
      {
        if (cachedEntityKey==null) {
          var domain = Session.Domain;
          var typeInfo = domain.Model.Types[EntityTypeId];
          cachedEntityKey = KeyFactory.CreateKey(domain, typeInfo, typeInfo.Key.TupleDescriptor.Parse(EntityKeyTuple));
        }
        return cachedEntityKey;
      }
    }

    /// <summary>
    /// Gets <see cref="TrackingItemState" /> for changed entity.
    /// </summary>
    [Field]
    public TrackingItemState ChangeKind { get; private set; }

    /// <summary>
    /// Creates new instance of <see cref="SyncLog"/> class.
    /// </summary>
    /// <param name="session">Session.</param>
    /// <param name="entityKey">Key of changed entity.</param>
    /// <param name="entityState">Kind change.</param>
    public SyncLog(Session session, Key entityKey, TrackingItemState entityState)
      : base(session)
    {
      if (entityKey==null)
        throw new ArgumentNullException("entityKey");

      EntityTypeId = entityKey.TypeInfo.TypeId;
      EntityKeyTuple = entityKey.Value.Format();
      ChangeKind = entityState;
    }
  }
}