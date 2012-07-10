﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Xtensive.Orm.Sync
{
  internal class MetadataStore<TEntity> : MetadataStore where TEntity : class, IEntity
  {
    public override Type EntityType
    {
      get { return typeof(TEntity); }
    }

    public override Type ItemType
    {
      get { return typeof(SyncInfo<TEntity>); }
    }

    public override IEnumerable<SyncInfo> GetMetadata(Expression filter)
    {
      var outer = Session.Query.All<SyncInfo<TEntity>>();
      var ex = filter as Expression<Func<TEntity, bool>>;
      var inner = Session.Query.All<TEntity>();
      if (ex != null)
        inner = inner.Where(ex);
      var items = outer
        .LeftJoin(inner, si => si.Entity.Key, t => t.Key, (si, t) => new { SyncInfo = si, Target = t })
        .ToArray();

      foreach (var item in UpdateItemState(items.Select(i => i.SyncInfo)))
        yield return item;
    }

    public override IEnumerable<SyncInfo> GetMetadata(List<Key> keys)
    {
      int batchCount = keys.Count / Wellknown.KeyPreloadBatchSize;
      int lastBatchItemCount = keys.Count % Wellknown.KeyPreloadBatchSize;
      if (lastBatchItemCount > 0)
        batchCount++;

      for (int i = 0; i < batchCount; i++) {
        var itemCount = Wellknown.KeyPreloadBatchSize;
        if (batchCount - i == 1 && lastBatchItemCount > 0)
          itemCount = lastBatchItemCount;

        var filter = FilterByKeys<TEntity>(keys, i*Wellknown.KeyPreloadBatchSize, itemCount);
        var items = Session.Query.All<SyncInfo<TEntity>>()
          .Where(filter)
          .Prefetch(s => s.Entity)
          .ToArray();

        foreach (var item in UpdateItemState(items))
          yield return item;
      }
    }

    public override SyncInfo GetMetadata(SyncInfo item)
    {
      UpdateItemState((SyncInfo<TEntity>) item);
      return item;
    }

    private Expression<Func<SyncInfo<TEntity>, bool>> FilterByKeys<T>(List<Key> keys, int start, int count)
    {
      var p = Expression.Parameter(typeof(SyncInfo<TEntity>), "p");
      var ea = Expression.Property(p, Wellknown.EntityFieldName);
      var ka = Expression.Property(ea, WellKnown.KeyFieldName);

      var body = Expression.Equal(ka, Expression.Constant(keys[start]));
      for (int i = 1; i < count; i++)
        body = Expression.OrElse(body, Expression.Equal(ka, Expression.Constant(keys[start+i])));

      return Expression.Lambda<Func<SyncInfo<TEntity>, bool>>(body, p);
    }

    protected IEnumerable<SyncInfo> UpdateItemState(IEnumerable<SyncInfo<TEntity>> items)
    {
      foreach (var item in items) {
        UpdateItemState(item);
        yield return item;
      }
    }

    private void UpdateItemState(SyncInfo<TEntity> item)
    {
      if (item.Entity!=null)
        item.SyncTargetKey = item.Entity.Key;
      else
        item.SyncTargetKey = EntityAccessor.GetReferenceKey(item, EntityField);
    }

    public MetadataStore(Session session)
      : base(session)
    {
    }
  }
}