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
      SyncId lastItemId;
      do {
        lastItemId = null;
        var items = ExecuteQuery(query, userFilter);
        foreach (var item in items) {
          lastItemId = item.SyncId;
          yield return item;
        }
        if (lastItemId!=null)
          query = query.ChangeMinId(SyncIdFormatter.GetNextId(lastItemId));
      }
      while (lastItemId!=null);
    }

    private IEnumerable<SyncInfo<TEntity>> ExecuteQuery(MetadataQuery query, Expression userFilter)
    {
      var outer = session.Query.All<SyncInfo<TEntity>>();
      var inner = session.Query.All<TEntity>();

      // Knowledge filter
      outer = outer.Where(GetFilter(query));

      // User filter
      var predicate = userFilter as Expression<Func<TEntity, bool>>;
      if (predicate!=null)
        inner = inner.Where(predicate);

      var itemQueryResult = outer
        .LeftJoin(inner, info => info.Entity, target => target, (info, target) => new {SyncInfo = info, Target = target})
        .OrderBy(pair => pair.SyncInfo.Id)
        .Take(WellKnown.OrderedMetadataBatchSize)
        .ToList();

      var keysToPrefetch = itemQueryResult
        .Where(p => p.Target!=null && !p.SyncInfo.IsTombstone)
        .Select(p => p.Target.Key);

      // To fetch entities
      PrefetchEntities(keysToPrefetch);

      return itemQueryResult.Select(p => p.SyncInfo);
    }

    public override IEnumerable<SyncInfo> GetUnorderedMetadata(List<Key> targetKeys)
    {
      const int batchSize = WellKnown.UnorderedMetadataBatchSize;

      int batchCount = targetKeys.Count / batchSize;
      int lastBatchItemCount = targetKeys.Count % batchSize;
      if (lastBatchItemCount > 0)
        batchCount++;

      for (int i = 0; i < batchCount; i++) {
        var itemCount = batchSize;
        if (batchCount - i==1 && lastBatchItemCount > 0)
          itemCount = lastBatchItemCount;

        var outer = session.Query.All<SyncInfo<TEntity>>();
        var inner = session.Query.All<TEntity>();

        outer = outer.Where(GetFilter(targetKeys, i * batchSize, itemCount));

        var itemQueryResult = outer
          .LeftJoin(inner, info => info.Entity, target => target, (info, target) => new {SyncInfo = info, Target = target})
          .ToList();

        var keysToFetch = itemQueryResult
          .Where(p => p.Target!=null && !p.SyncInfo.IsTombstone)
          .Select(p => p.Target.Key);

        // To fetch entities
        PrefetchEntities(keysToFetch);

        foreach (var item in itemQueryResult.Select(p => p.SyncInfo))
          yield return item;
      }
    }

    private void PrefetchEntities(IEnumerable<Key> keysToFetch)
    {
      session.Query.Many<TEntity>(keysToFetch).Run();
    }

    private Expression<Func<SyncInfo<TEntity>, bool>> GetFilter(List<Key> keys, int start, int count)
    {
      var info = Expression.Parameter(typeof (SyncInfo<TEntity>), "p");
      var entity = Expression.Property(info, WellKnown.EntityFieldName);
      var key = Expression.Property(entity, Orm.WellKnown.KeyFieldName);

      var body = Expression.Equal(key, Expression.Constant(keys[start]));
      for (int i = 1; i < count; i++)
        body = Expression.OrElse(body, Expression.Equal(key, Expression.Constant(keys[start + i])));

      return CreateFilter(info, body);
    }

    private Expression<Func<SyncInfo<TEntity>,bool>> GetFilter(MetadataQuery query)
    {
      var info = Expression.Parameter(typeof (SyncInfo<TEntity>), "p");
      var changeVersion = Expression.Property(info, "ChangeVersion");
      var id = Expression.Property(info, "Id");

      var minId = Expression.Constant(query.MinId.ToString());
      var maxId = Expression.Constant(query.MaxId.ToString());
      var zero = Expression.Constant(0);

      var rangeFilter = Expression.And(
        Expression.GreaterThanOrEqual(Expression.Call(id, WellKnown.StringCompareToMethod, minId), zero),
        Expression.LessThan(Expression.Call(id, WellKnown.StringCompareToMethod, maxId), zero));

      if (query.Filters==null)
        return CreateFilter(info, rangeFilter);

      var replica = Expression.Property(changeVersion, "Replica");
      var tick = Expression.Property(changeVersion, "Tick");

      var replicaFilter = query.Filters
        .Select(f => GetFilter(f, replica, tick))
        .Aggregate(Expression.Or);

      return CreateFilter(info, Expression.And(rangeFilter, replicaFilter));
    }

    private Expression GetFilter(MetadataQueryFilter filter, Expression replica, Expression tick)
    {
      var result = Expression.Equal(replica, Expression.Constant(filter.ReplicaKey));
      if (filter.LastKnownTick!=null)
        result = Expression.And(result, Expression.GreaterThan(tick, Expression.Constant(filter.LastKnownTick.Value)));
      return result;
    }

    private static Expression<Func<SyncInfo<TEntity>, bool>> CreateFilter(ParameterExpression info, Expression body)
    {
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
