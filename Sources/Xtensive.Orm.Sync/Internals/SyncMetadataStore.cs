using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Synchronization;
using Xtensive.Orm.Services;

namespace Xtensive.Orm.Sync
{
  internal class SyncMetadataStore : SessionBound
  {
    private readonly DirectEntityAccessor accessor;
    private readonly SyncRootSet syncRoots;
    private readonly SyncConfiguration configuration;

    public Replica Replica { get; private set; }

    public IEnumerable<ChangeSet> DetectChanges(uint batchSize, SyncKnowledge destinationKnowledge)
    {
      var mappedKnowledge = Replica.CurrentKnowledge.MapRemoteKnowledgeToLocal(destinationKnowledge);
      mappedKnowledge.ReplicaKeyMap.FindOrAddReplicaKey(Replica.Id);

      var filteredSyncRoots = syncRoots.AsEnumerable();
      if (configuration.Types.Count > 0)
        filteredSyncRoots = filteredSyncRoots.Where(r => configuration.Types.Contains(r.EntityType));

      foreach (var syncRoot in filteredSyncRoots) {
        var changeSets = DetectChanges(batchSize, mappedKnowledge, syncRoot);
        foreach (var changeSet in changeSets)
          yield return changeSet;
      }
    }

    private IEnumerable<ChangeSet> DetectChanges(uint batchSize, SyncKnowledge mappedKnowledge, SyncRoot syncRoot)
    {
      int itemCount = 0;
      var result = new ChangeSet();
      var references = new HashSet<Key>();
      var items = LoadMetadata(syncRoot);

      foreach (var item in items) {

        var createdVersion = item.CreationVersion;
        var lastChangeVersion = item.ChangeVersion;
        var changeKind = ChangeKind.Update;

        if (item.IsTombstone) {
          changeKind = ChangeKind.Deleted;
          lastChangeVersion = item.TombstoneVersion;
        }

        if (mappedKnowledge.Contains(Replica.Id, item.SyncId, lastChangeVersion))
          continue;

        var change = new ItemChange(Wellknown.IdFormats, Replica.Id, item.SyncId, changeKind, createdVersion, lastChangeVersion);
        var changeData = new ItemChangeData {
          Change = change,
          Identity = new Identity(item.GlobalId, item.SyncTargetKey),
        };
        if (!item.IsTombstone) {
          changeData.Tuple = accessor.GetEntityState(item.SyncTarget).Tuple.Clone();
          var type = item.SyncTargetKey.TypeInfo;
          var fields = type.Fields.Where(f => f.IsEntity);
          foreach (var field in fields) {
            var key = accessor.GetReferenceKey(item.SyncTarget, field);
            if (key!=null) {
              changeData.References.Add(field.Name, new Identity(key));
              references.Add(key);
              accessor.SetReferenceKey(item.SyncTarget, field, null);
            }
          }
        }
        result.Add(changeData);
        itemCount++;

        if (itemCount!=batchSize)
          continue;

        if (references.Count > 0)
          PreloadReferences(result, references);

        yield return result;
        itemCount = 0;
        result = new ChangeSet();
        references = new HashSet<Key>();
      }
      if (result.Any()) {
        if (references.Count > 0)
          PreloadReferences(result, references);
        yield return result;
      }
    }

    private void PreloadReferences(ChangeSet result, IEnumerable<Key> keys)
    {
      var lookup = LoadMetadata(keys.ToList())
        .Distinct()
        .ToDictionary(i => i.SyncTargetKey);
      foreach (var data in result)
        foreach (var reference in data.References.Values) {
          SyncInfo item;
          if (lookup.TryGetValue(reference.Key, out item)) {
            reference.Key = item.SyncTargetKey;
            reference.GlobalId = item.GlobalId;
          }
        }
    }

    public IEnumerable<ItemChange> GetLocalChanges(IEnumerable<ItemChange> sourceChanges)
    {
      // TODO: fix this

      var ids = sourceChanges
        .Select(i => i.ItemId.GetGuidId());
      var items = Session.Query.All<SyncInfo>()
        .Where(i => i.GlobalId.In(ids))
        .ToDictionary(i => i.GlobalId);

      foreach (var change in sourceChanges) {

        var changeKind = ChangeKind.UnknownItem;
        var createdVersion = SyncVersion.UnknownVersion;
        var lastChangeVersion = SyncVersion.UnknownVersion;
        
        SyncInfo info;
        if (items.TryGetValue(change.ItemId.GetGuidId(), out info)) {
          changeKind = ChangeKind.Update;
          createdVersion = info.CreationVersion;
          lastChangeVersion = info.ChangeVersion;
          if (info.IsTombstone) {
            changeKind = ChangeKind.Deleted;
            lastChangeVersion = info.TombstoneVersion;
          }
        }

        var localChange = new ItemChange(Wellknown.IdFormats, Replica.Id, change.ItemId, changeKind, createdVersion, lastChangeVersion);
        localChange.SetAllChangeUnitsPresent();
        yield return localChange;
      }
    }

    internal IEnumerable<SyncInfo> LoadMetadata(IEnumerable<Key> keys)
    {
      var groups = keys.GroupBy(i => i.TypeReference.Type.Hierarchy.Root);

      foreach (var @group in groups) {
        var syncRoot = syncRoots[@group.Key.UnderlyingType];
        if (syncRoot == null)
          continue;

        var items = LoadMetadataByKeys(syncRoot, @group.ToList());
        foreach (var item in items)
          yield return item;
      }
    }

    internal SyncInfo CreateMetadata(Key entityKey)
    {
      var syncRoot = syncRoots[entityKey.TypeReference.Type.UnderlyingType];
      if (syncRoot == null)
        return null;

      long tick = Replica.NextTick;
      var result = (SyncInfo) accessor.CreateEntity(syncRoot.ItemType);
      accessor.SetReferenceKey(result, syncRoot.EntityField, entityKey);
      result.CreatedReplicaKey = Wellknown.LocalReplicaKey;
      result.CreatedTickCount = tick;
      result.ChangeReplicaKey = Wellknown.LocalReplicaKey;
      result.ChangeTickCount = tick;
      result.GlobalId = Guid.NewGuid();
      return result;
    }

    internal void UpdateMetadata(SyncInfo item, bool markAsTombstone = false)
    {
      long tick = Replica.NextTick;
      item.ChangeReplicaKey = Wellknown.LocalReplicaKey;
      item.ChangeTickCount = tick;

      if (!markAsTombstone)
        return;

      item.TombstoneReplicaKey = Wellknown.LocalReplicaKey;
      item.TombstoneTickCount = tick;
      item.IsTombstone = true;
    }

    internal SyncInfo CreateMetadata(Key entityKey, ItemChange change)
    {
      var syncRoot = syncRoots[entityKey.TypeInfo.UnderlyingType];
      var result = (SyncInfo) accessor.CreateEntity(syncRoot.ItemType);
      accessor.SetReferenceKey(result, syncRoot.EntityField, entityKey);
      result.GlobalId = change.ItemId.GetGuidId();
      result.CreationVersion = change.CreationVersion;
      result.ChangeVersion = change.ChangeVersion;
      return result;
    }

    internal void UpdateMetadata(SyncInfo item, ItemChange change, bool markAsTombstone)
    {
      item.ChangeVersion = change.ChangeVersion;

      if (!markAsTombstone)
        return;

      item.TombstoneVersion = change.ChangeVersion;
      item.IsTombstone = true;
    }


    private IEnumerable<SyncInfo> LoadMetadata(SyncRoot syncRoot)
    {
      var mi = GetType().GetMethod("LoadMetadataImpl", BindingFlags.Instance|BindingFlags.NonPublic).MakeGenericMethod(syncRoot.EntityType);
      return (IEnumerable<SyncInfo>) mi.Invoke(this, new object[] {syncRoot});
    }

    private IEnumerable<SyncInfo> LoadMetadataImpl<T>(SyncRoot syncRoot) where T : Entity
    {
      var items = Session.Query.All<SyncInfo<T>>()
        .Prefetch(s => s.Entity)
        .ToArray();

      foreach (var item in items) {
        if (item.Entity != null)
          item.SyncTargetKey = item.Entity.Key;
        else
          item.SyncTargetKey = accessor.GetReferenceKey(item, syncRoot.EntityField);
        yield return item;
      }
    }


    private IEnumerable<SyncInfo> LoadMetadataByKeys(SyncRoot syncRoot, List<Key> keys)
    {
      var mi = GetType().GetMethod("LoadMetadataByKeysImpl", BindingFlags.Instance|BindingFlags.NonPublic).MakeGenericMethod(syncRoot.EntityType);
      return (IEnumerable<SyncInfo>) mi.Invoke(this, new object[] { syncRoot, keys });
    }

    private IEnumerable<SyncInfo> LoadMetadataByKeysImpl<T>(SyncRoot syncRoot, List<Key> keys) where T : Entity
    {
      int batchCount = keys.Count / Wellknown.KeyPreloadBatchSize;
      int lastBatchItemCount = keys.Count % Wellknown.KeyPreloadBatchSize;
      if (lastBatchItemCount > 0)
        batchCount++;

      for (int i = 0; i < batchCount; i++) {
        var itemCount = Wellknown.KeyPreloadBatchSize;
        if (batchCount - i == 1 && lastBatchItemCount > 0)
          itemCount = lastBatchItemCount;

        var filter = FilterByKeys<T>(keys, i, itemCount);
        var items = Session.Query.All<SyncInfo<T>>()
          .Where(filter)
          .Prefetch(s => s.Entity)
          .ToArray();
        foreach (var item in items) {
          if (item.Entity != null)
            item.SyncTargetKey = item.Entity.Key;
          else
            item.SyncTargetKey = accessor.GetReferenceKey(item, syncRoot.EntityField);
          yield return item;
        }
      }
    }

    private Expression<Func<SyncInfo<T>, bool>> FilterByKeys<T>(List<Key> keys, int start, int count) where T : Entity
    {
      var p = Expression.Parameter(typeof(SyncInfo<T>), "p");
      var ea = Expression.Property(p, "Entity");
      var ka = Expression.Property(ea, "Key");

      var body = Expression.Equal(ka, Expression.Constant(keys[start]));
      for (int i = 1; i < count; i++)
        body = Expression.OrElse(body, Expression.Equal(ka, Expression.Constant(keys[start+i])));

      return Expression.Lambda<Func<SyncInfo<T>, bool>>(body, p);
    }

    internal IEnumerable<SyncInfo> GetMetadataByGlobalId(SyncRoot syncRoot, IEnumerable<Guid> ids)
    {
      var mi = GetType().GetMethod("FetchMetadataByGlobalId").MakeGenericMethod(syncRoot.ItemType);
      return (IEnumerable<SyncInfo>) mi.Invoke(this, new object[] { syncRoot, ids });
    }

    private IEnumerable<SyncInfo> FetchMetadataByGlobalId<T>(SyncRoot syncRoot, Guid[] ids) where T : Entity
    {
      var items = Session.Query.All<SyncInfo<T>>()
        .Where(i => i.GlobalId.In(ids))
        .Prefetch(i => i.Entity)
        .ToArray();

      foreach (var item in items) {
        if (item.Entity != null)
          item.SyncTargetKey = item.Entity.Key;
        else
          item.SyncTargetKey = accessor.GetReferenceKey(item, syncRoot.EntityField);
        yield return item;
      }
    }

    public SyncMetadataStore(Session session, SyncRootSet syncRoots, SyncConfiguration configuration)
      : base(session)
    {
      this.syncRoots = syncRoots;
      this.configuration = configuration;
      accessor = session.Services.Get<DirectEntityAccessor>();
      Replica = new Replica(session);
    }
  }
}