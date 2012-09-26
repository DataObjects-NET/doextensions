using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Synchronization;
using Xtensive.Core;
using Xtensive.Orm.Sync.Model;

namespace Xtensive.Orm.Sync
{
  internal sealed class MetadataStore<TEntity> : MetadataStore
    where TEntity : class, IEntity
  {
    private readonly Session session;

    public override SyncInfo CreateMetadata(SyncId syncId, Key targetKey)
    {
      return new SyncInfo<TEntity>(session, syncId) {TargetKey = targetKey};
    }

    public override IEnumerable<SyncInfo> GetOrderedMetadata(MetadataQuery query, Expression userFilter)
    {
      var outer = session.Query.All<SyncInfo<TEntity>>();
      var inner = session.Query.All<TEntity>();

      // Range filter
      if (query.MinId!=null && query.MaxId!=null)
        outer = outer.Where(info => info.Id.GreaterThanOrEqual(query.MinId.ToString()) && info.Id.LessThan(query.MaxId.ToString()));

      // Replica and tick filter
      if (query.ReplicaKey!=null) {
        outer = outer.Where(info => info.ChangeVersion.Replica==query.ReplicaKey.Value);
        if (query.LastKnownTick!=null)
          outer = outer.Where(info => info.ChangeVersion.Tick > query.LastKnownTick.Value);
      }

      // User filter
      var predicate = userFilter as Expression<Func<TEntity, bool>>;
      if (predicate!=null)
        inner = inner.Where(predicate);

      var itemQueryResult = outer
        .LeftJoin(
          inner, info => info.Entity, target => target, (info, target) => new {SyncInfo = info, Target = target})
        .Where(pair => pair.Target!=null || pair.SyncInfo.IsTombstone)
        .OrderBy(pair => pair.SyncInfo.Id)
        .ToList();
      var keysToPrefetch = itemQueryResult
        .Where(p => !p.SyncInfo.IsTombstone)
        .Select(p => p.Target.Key)
        .ToList();
      var items = itemQueryResult.Select(p => p.SyncInfo);

      // To fetch entities
      session.Query.Many<TEntity>(keysToPrefetch).Run();
      return items;
    }

    public override IEnumerable<SyncInfo> GetUnorderedMetadata(List<Key> targetKeys)
    {
      int batchCount = targetKeys.Count / WellKnown.UnorderedMetadataFetchBatchSize;
      int lastBatchItemCount = targetKeys.Count % WellKnown.UnorderedMetadataFetchBatchSize;
      if (lastBatchItemCount > 0)
        batchCount++;

      for (int i = 0; i < batchCount; i++) {
        var itemCount = WellKnown.UnorderedMetadataFetchBatchSize;
        if (batchCount - i==1 && lastBatchItemCount > 0)
          itemCount = lastBatchItemCount;

        var outer = session.Query.All<SyncInfo<TEntity>>();
        var inner = session.Query.All<TEntity>();
        var filter = FilterByKeys(targetKeys, i * WellKnown.UnorderedMetadataFetchBatchSize, itemCount);
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
        session.Query.Many<TEntity>(keysToFetch).Run();
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

    public MetadataStore(Session session, SyncId minItemId, SyncId maxItemId)
      : base(typeof (TEntity), minItemId, maxItemId)
    {
      if (session==null)
        throw new ArgumentNullException("session");
      this.session = session;
    }
  }
}
