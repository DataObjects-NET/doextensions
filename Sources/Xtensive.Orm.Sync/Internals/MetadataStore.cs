using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Synchronization;
using Xtensive.Orm.Sync.Model;

namespace Xtensive.Orm.Sync
{
  internal abstract class MetadataStore
  {
    public Type EntityType { get; private set; }

    public SyncId MinItemId { get; private set; }

    public SyncId MaxItemId { get; private set; }

    public abstract SyncInfo CreateMetadata(SyncId syncId, Key targetKey);

    public abstract IEnumerable<SyncInfo> GetOrderedMetadata(MetadataQuery query, Expression userFilter);

    public abstract IEnumerable<SyncInfo> GetUnorderedMetadata(List<Key> targetKeys);

    public abstract void ForgetMetadata();

    public abstract void CreateMissingMetadata();

    protected MetadataStore(Type entityType, SyncId minItemId, SyncId maxItemId)
    {
      if (entityType==null)
        throw new ArgumentNullException("entityType");
      if (minItemId==null)
        throw new ArgumentNullException("minItemId");
      if (maxItemId==null)
        throw new ArgumentNullException("maxItemId");

      EntityType = entityType;

      MinItemId = minItemId;
      MaxItemId = maxItemId;
    }
  }
}
