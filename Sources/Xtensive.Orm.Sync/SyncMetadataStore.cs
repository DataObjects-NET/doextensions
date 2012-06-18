using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Serialization;
using Microsoft.Synchronization;
using Xtensive.Orm.Metadata;
using Xtensive.Orm.Services;

namespace Xtensive.Orm.Sync
{
  public class SyncMetadataStore : SessionBound
  {
    private readonly DirectEntityAccessor accessor;
    private readonly SyncTickGenerator tickGenerator;
    private readonly SyncRootSet syncRoots;

    public SyncIdFormatGroup IdFormats { get { return Wellknown.IdFormats; } }

    public SyncId ReplicaId { get; private set; }

    public SyncKnowledge CurrentKnowledge { get; private set; }

    public ForgottenKnowledge ForgottenKnowledge { get; private set; }

    public long TickCount
    {
      get { return tickGenerator.GetLastTick(Session); }
    }

    public long NextTick
    {
      get { return tickGenerator.GetNextTick(Session); }
    }

    public IEnumerable<ChangeSet> DetectChanges(uint batchSize, SyncKnowledge destinationKnowledge)
    {
      var mappedKnowledge = CurrentKnowledge.MapRemoteKnowledgeToLocal(destinationKnowledge);
      mappedKnowledge.ReplicaKeyMap.FindOrAddReplicaKey(CurrentKnowledge.ReplicaId);

      foreach (var syncRoot in syncRoots) {
        var mi = GetType().GetMethod("DetectChangesForType", BindingFlags.NonPublic|BindingFlags.Instance).MakeGenericMethod(syncRoot.EntityType);
        var changes = (IEnumerable<ChangeSet>) mi.Invoke(this, new object[] { batchSize, mappedKnowledge });
        foreach (var change in changes)
          yield return change;
      }
    }

    private IEnumerable<ChangeSet> DetectChangesForType<TEntity> (uint batchSize, SyncKnowledge mappedKnowledge) where TEntity : Entity
    {
      int itemCount = 0;
      var result = new ChangeSet();
      var keys = new HashSet<Key>();
      foreach (var item in Session.Query.All<SyncInfo<TEntity>>().Prefetch(i => i.Entity)) {

        var createdVersion = item.CreationVersion;
        var lastChangeVersion = item.ChangeVersion;
        var changeKind = ChangeKind.Update;

        if (item.IsTombstone) {
          changeKind = ChangeKind.Deleted;
          lastChangeVersion = item.TombstoneVersion;
        }

        if (mappedKnowledge.Contains(ReplicaId, item.SyncId, lastChangeVersion))
          continue;

        var change = new ItemChange(IdFormats, ReplicaId, item.SyncId, changeKind, createdVersion, lastChangeVersion);
        var changeData = new ItemChangeData {
          Change = change,
          Identity = new Identity(item.GlobalId, item.Entity.Key),
        };
        if (!item.IsTombstone) {
          changeData.Tuple = accessor.GetEntityState(item.Entity).Tuple.Clone();
          var type = Session.Domain.Model.Types[item.Entity.GetType()];
          var fields = type.Fields.Where(f => f.IsEntity);
          foreach (var field in fields) {
            var key = accessor.GetReferenceKey(item.Entity, field);
            if (key!=null) {
              changeData.References.Add(field.Name, new Identity(key));
              keys.Add(key);
              accessor.SetReferenceKey(item.Entity, field, null);
            }
          }
        }
        result.Add(changeData);
        itemCount++;

        if (itemCount!=batchSize)
          continue;

        // Getting global ids for referenced entities
        if (keys.Count > 0) {
          var lookup = GetMetadata(keys.ToList())
            .Distinct()
            .ToDictionary(i => i.Key, i => i.GlobalId);
          foreach (var data in result) {
            foreach (var reference in data.References.Values) {
              Guid globalId;
              if (lookup.TryGetValue(reference.Key, out globalId))
                reference.GlobalId = globalId;
            }
          }
        }

        yield return result;
        itemCount = 0;
        result = new ChangeSet();
        keys = new HashSet<Key>();
      }
      if (result.Any())
        yield return result;
    }

    public IEnumerable<ItemChange> GetLocalChanges(IEnumerable<ItemChange> sourceChanges)
    {
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

        var localChange = new ItemChange(IdFormats, ReplicaId, change.ItemId, changeKind, createdVersion, lastChangeVersion);
        localChange.SetAllChangeUnitsPresent();
        yield return localChange;
      }
    }

    internal IEnumerable<SyncInfo> GetMetadata(IEnumerable<Key> keys)
    {
      var groups = keys.GroupBy(i => i.TypeInfo.Hierarchy.Root);

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

      long tick = NextTick;
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
      long tick = NextTick;
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

//    internal void UpdateMetadata(Type entityType, Key entityKey, ItemChange change, bool markAsTombstone = false)
//    {
//      var syncRoot = SyncRoots.GetSyncRoot(entityKey.TypeInfo.UnderlyingType);
//      var result = GetMetadataByEntityKey(syncRoot, entityKey).SingleOrDefault();
//      if (result==null)
//        result = CreateMetadata(entityType, entityKey, change);
//
//      result.ChangeVersion = change.ChangeVersion;
//
//      if (!markAsTombstone)
//        return;
//
//      result.TombstoneVersion = change.ChangeVersion;
//      result.IsTombstone = true;
//    }

    private IEnumerable<SyncInfo> LoadMetadataByKeys(SyncRoot syncRoot, List<Key> keys)
    {
      var mi = GetType().GetMethod("LoadMetadataByKeysImpl", BindingFlags.Instance|BindingFlags.NonPublic).MakeGenericMethod(syncRoot.EntityType);
      return (IEnumerable<SyncInfo>) mi.Invoke(this, new object[] { keys });
    }

    private IEnumerable<SyncInfo> LoadMetadataByKeysImpl<T>(List<Key> keys) where T : Entity
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
        var result = Session.Query.All<SyncInfo<T>>()
          .Where(filter)
          .Prefetch(s => s.Entity)
          .ToArray();
        foreach (var item in result)
          yield return item;
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
      return (IEnumerable<SyncInfo>) mi.Invoke(this, new object[] { ids });
    }

    private IEnumerable<SyncInfo> FetchMetadataByGlobalId<T>(Guid[] ids) where T : Entity
    {
      return Session.Query.All<SyncInfo<T>>()
        .Where(i => i.GlobalId.In(ids))
        .Prefetch(i => i.Entity);
    }

    #region Initialization & knowledge update bits

    private void ReadKnowledge()
    {
      var names = new[] {Wellknown.FieldNames.ReplicaId, Wellknown.FieldNames.CurrentKnowledge, Wellknown.FieldNames.ForgottenKnowledge};
      var values = Session.Query.All<Extension>()
        .Where(e => e.Name.In(names)).ToArray();

      var value = values.SingleOrDefault(v => v.Name==Wellknown.FieldNames.ReplicaId);
      if (value==null) {
        ReplicaId = new SyncId(Guid.NewGuid());
        using (Session.Activate())
          new Extension(Wellknown.FieldNames.ReplicaId) {
            Text = ReplicaId.GetGuidId().ToString()
          };
      }
      else {
        try { ReplicaId = new SyncId(new Guid(value.Text)); }
        catch (Exception) { }
      }

      value = values.SingleOrDefault(v => v.Name==Wellknown.FieldNames.CurrentKnowledge);
      if (value!=null) {
        CurrentKnowledge = Deserialize<SyncKnowledge>(value.Text);
        CurrentKnowledge.SetLocalTickCount((ulong) TickCount);
      }
      else
        CurrentKnowledge = new SyncKnowledge(IdFormats, ReplicaId, (ulong) TickCount);

      value = values.SingleOrDefault(v => v.Name==Wellknown.FieldNames.ForgottenKnowledge);
      if (value!=null)
        ForgottenKnowledge = Deserialize<ForgottenKnowledge>(value.Text);
      else
        ForgottenKnowledge = new ForgottenKnowledge(IdFormats, CurrentKnowledge);
    }

    internal void UpdateKnowledge(SyncKnowledge syncKnowledge, ForgottenKnowledge forgottenKnowledge)
    {
      if (syncKnowledge==null)
        throw new ArgumentNullException("syncKnowledge");

      var names = new[] {Wellknown.FieldNames.CurrentKnowledge, Wellknown.FieldNames.ForgottenKnowledge};
      var values = Session.Query.All<Extension>()
        .Where(e => e.Name.In(names)).ToArray();

      var value = values.SingleOrDefault(e => e.Name==Wellknown.FieldNames.CurrentKnowledge);
      if (value==null)
        using (Session.Activate())
          value = new Extension(Wellknown.FieldNames.CurrentKnowledge);
      value.Text = Serialize(syncKnowledge);

      if (forgottenKnowledge == null)
        return;

      value = values.SingleOrDefault(e => e.Name==Wellknown.FieldNames.ForgottenKnowledge);
      if (value==null)
        using (Session.Activate())
          value = new Extension(Wellknown.FieldNames.ForgottenKnowledge);
      value.Text = Serialize(forgottenKnowledge);
    }

    private static T Deserialize<T>(string value)
    {
      using (var reader = new StringReader(value)) {
        var serializer = new XmlSerializer(typeof (T));
        return (T) serializer.Deserialize(reader);
      }
    }

    private static string Serialize<T>(T value)
    {
      using (var writer = new StringWriter()) {
        var serializer = new XmlSerializer(typeof (T));
        serializer.Serialize(writer, value);
        return writer.ToString();
      }
    }

    #endregion

    public SyncMetadataStore(Session session, SyncRootSet syncRoots)
      : base(session)
    {
      this.syncRoots = syncRoots;
      accessor = session.Services.Get<DirectEntityAccessor>();
      if (tickGenerator == null)
        tickGenerator = session.Domain.Services.Get<SyncTickGenerator>();
      
      ReadKnowledge();
   }
  }
}