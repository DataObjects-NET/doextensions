using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Synchronization;
using Xtensive.Orm.Sync.Model;

namespace Xtensive.Orm.Sync
{
  internal abstract class MetadataStore
  {
    public Session Session { get; private set; }

    public Type EntityType { get; private set; }

    public abstract SyncInfo CreateMetadata(SyncId syncId, Key targetKey);

    public abstract IEnumerable<SyncInfo> GetOrderedMetadata(Expression filter);

    public abstract IEnumerable<SyncInfo> GetUnorderedMetadata(List<Key> targetKeys);

    protected MetadataStore(Session session, Type entityType)
    {
      if (session==null)
        throw new ArgumentNullException("session");
      if (entityType==null)
        throw new ArgumentNullException("entityType");

      Session = session;
      EntityType = entityType;
    }
  }
}
