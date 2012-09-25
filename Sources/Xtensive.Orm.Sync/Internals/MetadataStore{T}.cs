using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Synchronization;
using Xtensive.Core;
using Xtensive.Orm.Sync.Model;

namespace Xtensive.Orm.Sync
{
  internal sealed class MetadataStore<TEntity> : MetadataStore
    where TEntity : class, IEntity
  {
    public override SyncInfo CreateMetadata(SyncId syncId, Key targetKey)
    {
      return new SyncInfo<TEntity>(Session, syncId) {TargetKey = targetKey};
    }

    public override IEnumerable<SyncInfo> GetOrderedMetadata(IMetadataQuery query)
    {
      var outer = Session.Query.All<SyncInfo<TEntity>>();
      var inner = Session.Query.All<TEntity>();

      // Range filter
      if (query.MinId!=null && query.MaxId!=null)
        outer = outer.Where(info => info.Id.GreaterThanOrEqual(query.MinId) && info.Id.LessThan(query.MaxId));

      // Known versions filter
      if (query.LastKnownVersion!=null)
        outer = outer.Where(info =>
          info.ChangeVersion.Replica==query.LastKnownVersion.ReplicaKey
          && info.ChangeVersion.Tick > (long) query.LastKnownVersion.TickCount);

      // Unknown relicas filter
      if (query.ReplicasToExclude!=null && query.ReplicasToExclude.Count > 0)
        outer = outer.Where(info => !query.ReplicasToExclude.Contains(info.ChangeVersion.Replica));

      // User filter
      var predicate = query.UserFilter as Expression<Func<TEntity, bool>>;
      if (predicate!=null)
        inner = inner.Where(predicate);

      var itemQueryResult = outer
        .LeftJoin(inner, info => info.Entity, target => target, (info, target) => new {SyncInfo = info, Target = target})
        .Where(pair => pair.Target!=null || pair.SyncInfo.IsTombstone)
        .OrderBy(pair => pair.SyncInfo.Id)
        .ToList();
      var keysToPrefetch = itemQueryResult
        .Where(p => !p.SyncInfo.IsTombstone)
        .Select(p => p.Target.Key)
        .ToList();
      var items = itemQueryResult.Select(p => p.SyncInfo);

      // To fetch entities
      Session.Query.Many<TEntity>(keysToPrefetch).Run();
      return items;
    }

    public override IEnumerable<SyncInfo> GetUnorderedMetadata(List<Key> targetKeys)
    {
      int batchCount = targetKeys.Count / WellKnown.EntityFetchBatchSize;
      int lastBatchItemCount = targetKeys.Count % WellKnown.EntityFetchBatchSize;
      if (lastBatchItemCount > 0)
        batchCount++;

      for (int i = 0; i < batchCount; i++) {
        var itemCount = WellKnown.EntityFetchBatchSize;
        if (batchCount - i==1 && lastBatchItemCount > 0)
          itemCount = lastBatchItemCount;

        var outer = Session.Query.All<SyncInfo<TEntity>>();
        var inner = Session.Query.All<TEntity>();
        var filter = FilterByKeys(targetKeys, i * WellKnown.EntityFetchBatchSize, itemCount);
        var itemQueryResult = outer
          .Where(filter)
          .LeftJoin(inner, info => info.Entity, target => target, (info, target) => new {SyncInfo = info, Target = target})
          .ToList();
        var keysToFetch = itemQueryResult
          .Where(p => !p.SyncInfo.IsTombstone && p.Target!=null)
          .Select(p => p.Target.Key)
          .ToList();
        var items = itemQueryResult.Select(p => p.SyncInfo);

        // To fetch entities
        Session.Query.Many<TEntity>(keysToFetch).Run();
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
