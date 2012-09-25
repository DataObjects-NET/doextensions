using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Synchronization;
using Xtensive.Orm.Services;
using Xtensive.Orm.Sync.DataExchange;
using Xtensive.Orm.Sync.Model;

namespace Xtensive.Orm.Sync
{
  internal sealed class ChangeDetector
  {
    private readonly ReplicaInfo replicaInfo;
    private readonly MetadataManager metadataManager;
    private readonly DirectEntityAccessor entityAccessor;
    private readonly EntityTupleFormatterRegistry tupleFormatters;
    private readonly KeyTracker keyTracker;
    private readonly MetadataQueryBuilder queryBuilder;

    public IEnumerable<ChangeSet> DetectChanges(uint batchSize, SyncKnowledge destinationKnowledge)
    {
      var mappedKnowledge = replicaInfo.CurrentKnowledge.MapRemoteKnowledgeToLocal(destinationKnowledge);
      mappedKnowledge.ReplicaKeyMap.FindOrAddReplicaKey(replicaInfo.Id);

      foreach (var queryGroup in queryBuilder.GetQueryGroups(mappedKnowledge)) {
        queryGroup.Dump();
        var nextChangeSetLowerBound = queryGroup.MinResultId;
        var batches = DetectChangesForItems(queryGroup.ExecuteAll(), batchSize, mappedKnowledge, true);
        foreach (var batch in batches) {
          batch.MinId = nextChangeSetLowerBound;
          nextChangeSetLowerBound = SyncIdFormatter.GetNextId(batch.MaxId);
          yield return batch;
        }

        yield return new ChangeSet(true) {
          MinId = nextChangeSetLowerBound,
          MaxId = queryGroup.MaxResultId
        };
      }

      while (keyTracker.HasKeysToSync) {
        var keys = keyTracker.GetKeysToSync();
        var groups = keys.GroupBy(i => i.TypeReference.Type.Hierarchy.Root);
        foreach (var group in groups) {
          var items = metadataManager.GetStore(group.Key).GetUnorderedMetadata(group.ToList());
          var batches = DetectChangesForItems(items, batchSize, mappedKnowledge, false);
          foreach (var batch in batches)
            yield return batch;
        }
      }
    }

    private IEnumerable<ChangeSet> DetectChangesForItems(
      IEnumerable<SyncInfo> items, uint batchSize, SyncKnowledge destinationKnowledge, bool isOrdered)
    {
      var result = new ChangeSet(isOrdered);
      var references = new HashSet<Key>();

      foreach (var item in items) {
        var changeData = GetChangeData(destinationKnowledge, item, references);
        if (changeData==null)
          continue;
        result.Add(changeData);

        if (result.Count!=batchSize)
          continue;

        if (references.Count > 0)
          LoadReferences(result, references);

        yield return result;

        result = new ChangeSet(isOrdered);
        references = new HashSet<Key>();
      }

      if (result.Count > 0) {
        if (references.Count > 0)
          LoadReferences(result, references);
        yield return result;
      }
    }

    private ItemChangeData GetChangeData(SyncKnowledge destinationKnowledge, SyncInfo item, HashSet<Key> references)
    {
      var createdVersion = item.CreationVersion.Version;
      var lastChangeVersion = item.ChangeVersion.Version;
      var changeKind = item.IsTombstone ? ChangeKind.Deleted : ChangeKind.Update;

      if (destinationKnowledge.Contains(replicaInfo.Id, item.SyncId, lastChangeVersion)) {
        keyTracker.UnrequestKeySync(item.TargetKey);
        return null;
      }

      var change = new ItemChange(
        metadataManager.IdFormats, replicaInfo.Id, item.SyncId,
        changeKind, createdVersion, lastChangeVersion);

      var changeData = new ItemChangeData {
        Change = change,
        Identity = new Identity(item.TargetKey, item.SyncId),
      };

      if (item.IsTombstone)
        return changeData;

      keyTracker.RegisterKeySync(item.TargetKey);
      var syncTarget = item.Target;
      var entityTuple = entityAccessor.GetEntityState(syncTarget).Tuple;
      changeData.TupleValue = tupleFormatters.Get(syncTarget.TypeInfo.UnderlyingType).Format(entityTuple);
      var type = item.TargetKey.TypeInfo;
      var fields = type.Fields.Where(f => f.IsEntity);
      foreach (var field in fields) {
        var key = entityAccessor.GetReferenceKey(syncTarget, field);
        if (key==null)
          continue;
        changeData.References.Add(field.Name, new Identity(key));
        references.Add(key);
        entityAccessor.SetReferenceKey(syncTarget, field, null);
      }

      return changeData;
    }

    private void LoadReferences(IEnumerable<ItemChangeData> items, IEnumerable<Key> keys)
    {
      var metadataSet = metadataManager.GetMetadata(keys);

      foreach (var item in items)
        foreach (var reference in item.References.Values) {
          var metadata = metadataSet[reference.Key];
          if (metadata==null)
            continue;
          reference.Key = metadata.TargetKey;
          reference.GlobalId = metadata.SyncId;
          keyTracker.RequestKeySync(metadata.TargetKey);
        }
    }

    public ChangeDetector(
      ReplicaInfo replicaInfo, SyncConfiguration configuration,
      MetadataManager metadataManager, DirectEntityAccessor entityAccessor,
      EntityTupleFormatterRegistry tupleFormatters)
    {
      if (replicaInfo==null)
        throw new ArgumentNullException("replicaInfo");
      if (metadataManager==null)
        throw new ArgumentNullException("metadataManager");
      if (configuration==null)
        throw new ArgumentNullException("configuration");
      if (entityAccessor==null)
        throw new ArgumentNullException("entityAccessor");
      if (tupleFormatters==null)
        throw new ArgumentNullException("tupleFormatters");

      this.replicaInfo = replicaInfo;
      this.metadataManager = metadataManager;
      this.entityAccessor = entityAccessor;
      this.tupleFormatters = tupleFormatters;

      keyTracker = new KeyTracker(configuration);
      queryBuilder = new MetadataQueryBuilder(metadataManager, configuration);
    }
  }
}