using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Synchronization;
using Xtensive.Orm.Model;
using Xtensive.Orm.Services;

namespace Xtensive.Orm.Sync
{
  internal abstract class MetadataStore : SessionBound
  {
    public abstract Type InfoType { get; }

    public abstract Type EntityType { get; }

    public FieldInfo EntityField { get; private set; }

    public DirectEntityAccessor EntityAccessor { get; private set; }

    public abstract SyncInfo GetMetadata(SyncInfo item);

    public abstract IEnumerable<SyncInfo> GetMetadata(List<Key> keys);

    public abstract IEnumerable<SyncInfo> GetMetadata(Expression filter);

    public SyncInfo CreateMetadata(SyncId syncId, Key targetKey)
    {
      var result = (SyncInfo) Activator.CreateInstance(InfoType, Session, syncId);
      EntityAccessor.SetReferenceKey(result, EntityField, targetKey);
      return result;
    }

    protected MetadataStore(Session session)
      : base(session)
    {
      var typeInfo = session.Domain.Model.Types[InfoType];
      EntityField = typeInfo.Fields[WellKnown.EntityFieldName];
      EntityAccessor = session.Services.Get<DirectEntityAccessor>();
    }
  }
}
