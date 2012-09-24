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
    private static MethodInfo EnumerableContains = typeof (Enumerable).GetMethods()
      .Single(m => m.Name=="Contains" && m.GetParameters().Length==2)
      .MakeGenericMethod(typeof (uint));

    public override SyncInfo CreateMetadata(SyncId syncId, Key targetKey)
    {
      return new SyncInfo<TEntity>(Session, syncId) {TargetKey = targetKey};
    }

    public override IEnumerable<SyncInfo> GetOrderedMetadata(IMetadataQuery query)
    {
      var outer = Session.Query.All<SyncInfo<TEntity>>();
      var inner = Session.Query.All<TEntity>();

      if (query.MinId!=null && query.MaxId!=null)
        outer = outer.Where(info => info.Id.GreaterThanOrEqual(query.MinId) && info.Id.LessThan(query.MaxId));
      if (query.LastKnownVersions!=null && query.LastKnownVersions.Count > 0)
        outer = outer.Where(GetVersionFilter(query.LastKnownVersions));

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

    private Expression<Func<SyncInfo<TEntity>, bool>> GetVersionFilter(IList<SyncVersion> lastKnownVersions)
    {
      var info = Expression.Parameter(typeof (SyncInfo<TEntity>), "p");
      var changeVersion = Expression.Property(info, "ChangeVersion");
      var replica = Expression.Property(changeVersion, "Replica");
      var tick = Expression.Property(changeVersion, "Tick");

      var body = GetVersionFilter(tick, replica, lastKnownVersions[0]);
      for (int i = 1; i < lastKnownVersions.Count; i++)
        body = Expression.OrElse(body, GetVersionFilter(tick, replica, lastKnownVersions[i]));

      var replicaKeys = Expression.Constant(lastKnownVersions.Select(i => i.ReplicaKey).ToArray());

      body = Expression.OrElse(body,
        Expression.Not(Expression.Call(EnumerableContains, replicaKeys, replica)));

      return Expression.Lambda<Func<SyncInfo<TEntity>, bool>>(body, info);
    }

    private static Expression GetVersionFilter(Expression tick, Expression replica, SyncVersion version)
    {
      var replicaValue = Expression.Constant(version.ReplicaKey);
      var tickValue = Expression.Constant((long) version.TickCount);

      return Expression.And(
        Expression.Equal(replica, replicaValue),
        Expression.GreaterThan(tick, tickValue));
    }

    public MetadataStore(Session session)
      : base(session, typeof (TEntity))
    {
    }
  }
}
