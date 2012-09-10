using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Synchronization;
using Xtensive.Core;

namespace Xtensive.Orm.Sync
{
  internal sealed class MetadataStore<TEntity> : MetadataStore
    where TEntity : class, IEntity
  {
    public override SyncInfo CreateMetadata(SyncId syncId, Key targetKey)
    {
      return new SyncInfo<TEntity>(Session, syncId) {SyncTargetKey = targetKey};
    }

    public override IEnumerable<SyncInfo> GetMetadata(Expression filter)
    {
      var outer = Session.Query.All<SyncInfo<TEntity>>();
      var inner = Session.Query.All<TEntity>();

      var predicate = filter as Expression<Func<TEntity, bool>>;
      if (predicate!=null)
        inner = inner.Where(predicate);

      var pairs = outer
        .LeftJoin(inner, info => info.Entity, target => target, (info, target) => new {SyncInfo = info, Target = target})
        .Where(pair => pair.Target!=null || pair.SyncInfo.IsTombstone)
        .ToList();
      var fetchedKeys = pairs
        .Where(p => !p.SyncInfo.IsTombstone)
        .Select(p => p.Target.Key)
        .ToList();
      var items = pairs
        .Select(p => p.SyncInfo);

      // To fetch entities
      Session.Query.Many<TEntity>(fetchedKeys).Run();
      return items;
    }

    public override IEnumerable<SyncInfo> GetMetadata(List<Key> targetKeys)
    {
      int batchCount = targetKeys.Count / WellKnown.KeyPreloadBatchSize;
      int lastBatchItemCount = targetKeys.Count % WellKnown.KeyPreloadBatchSize;
      if (lastBatchItemCount > 0)
        batchCount++;

      for (int i = 0; i < batchCount; i++) {
        var itemCount = WellKnown.KeyPreloadBatchSize;
        if (batchCount - i==1 && lastBatchItemCount > 0)
          itemCount = lastBatchItemCount;

        var outer = Session.Query.All<SyncInfo<TEntity>>();
        var inner = Session.Query.All<TEntity>();
        var filter = FilterByKeys(targetKeys, i * WellKnown.KeyPreloadBatchSize, itemCount);
        var pairs = outer
          .Where(filter)
          .LeftJoin(inner, info => info.Entity, target => target, (info, target) => new {SyncInfo = info, Target = target})
          .ToList();
        var fetchedKeys = pairs
          .Where(p => !p.SyncInfo.IsTombstone && p.Target!=null)
          .Select(p => p.Target.Key)
          .ToList();
        var items = pairs
          .Select(p => p.SyncInfo);

        // To fetch entities
        Session.Query.Many<TEntity>(fetchedKeys).Run();
        foreach (var item in items)
          yield return item;
      }
    }

    private Expression<Func<SyncInfo<TEntity>, bool>> FilterByKeys(List<Key> keys, int start, int count)
    {
      var info = Expression.Parameter(typeof (SyncInfo<TEntity>), "p");
      var entity = Expression.Property(info, WellKnown.EntityFieldName);
      var key = Expression.Property(entity, Orm.WellKnown.KeyFieldName);

      var body = Expression.Equal(key, Expression.Constant(keys[start]));
      for (int i = 1; i < count; i++)
        body = Expression.OrElse(body, Expression.Equal(key, Expression.Constant(keys[start + i])));

      return Expression.Lambda<Func<SyncInfo<TEntity>, bool>>(body, info);
    }

    public MetadataStore(Session session)
      : base(session, typeof (TEntity))
    {
    }
  }
}
