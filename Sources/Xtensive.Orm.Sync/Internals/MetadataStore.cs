using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xtensive.Orm.Model;
using Xtensive.Orm.Services;

namespace Xtensive.Orm.Sync
{
  internal abstract class MetadataStore : SessionBound
  {
    public abstract Type ItemType { get; }

    public abstract Type EntityType { get; }

    public FieldInfo EntityField { get; private set; }

    public DirectEntityAccessor EntityAccessor { get; private set; }

    public abstract IEnumerable<SyncInfo> GetMetadata(Expression filter);

    public abstract IEnumerable<SyncInfo> GetMetadata(List<Key> keys);

    public abstract SyncInfo GetMetadata(SyncInfo item);

    public SyncInfo CreateItem(Key key)
    {
      var result = (SyncInfo) EntityAccessor.CreateEntity(ItemType);
      EntityAccessor.SetReferenceKey(result, EntityField, key);
      return result;
    }

    protected MetadataStore(Session session)
      : base(session)
    {
      var typeInfo = session.Domain.Model.Types[ItemType];
      EntityField = typeInfo.Fields[WellKnown.EntityFieldName];
      EntityAccessor = session.Services.Get<DirectEntityAccessor>();
    }
  }
}
