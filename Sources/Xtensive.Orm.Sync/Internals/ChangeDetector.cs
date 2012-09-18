using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Synchronization;
using Xtensive.Orm.Services;
using Xtensive.Orm.Sync.DataExchange;
using Xtensive.Orm.Sync.Model;

namespace Xtensive.Orm.Sync
{
  internal sealed class ChangeDetector
  {
    private readonly ReplicaState replicaState;
    private readonly MetadataManager metadataManager;
    private readonly SyncConfiguration configuration;
    private readonly DirectEntityAccessor entityAccessor;
    private readonly EntityTupleFormatterRegistry tupleFormatters;
    private readonly KeyTracker keyTracker;

    public IEnumerable<ChangeSet> DetectChanges(uint batchSize, SyncKnowledge destinationKnowledge)
    {
      var mappedKnowledge = replicaState.CurrentKnowledge.MapRemoteKnowledgeToLocal(destinationKnowledge);
      mappedKnowledge.ReplicaKeyMap.FindOrAddReplicaKey(replicaState.Id);

      foreach (var store in metadataManager.GetStores(configuration)) {
        Expression filter;
        configuration.Filters.TryGetValue(store.EntityType, out filter);
        var items = store.GetOrderedMetadata(filter);
        var batches = DetectChanges(items, batchSize, mappedKnowledge, true);
        foreach (var batch in batches)
          yield return batch;
      }

      while (keyTracker.HasKeysToSync) {
        var keys = keyTracker.GetKeysToSync();
        var groups = keys.GroupBy(i => i.TypeReference.Type.Hierarchy.Root);
        foreach (var group in groups) {
          var items = metadataManager.GetStore(group.Key).GetUnorderedMetadata(group.ToList());
          var batches = DetectChanges(items, batchSize, mappedKnowledge, false);
          foreach (var batch in batches)
            yield return batch;
        }
      }
    }

    private IEnumerable<ChangeSet> DetectChanges(IEnumerable<SyncInfo> items, uint batchSize, SyncKnowledge mappedKnowledge, bool isOrdered)
    {
      var result = new ChangeSet(isOrdered);
      var references = new HashSet<Key>();

      foreach (var item in items) {
        var createdVersion = item.CreationVersion.Version;
        var lastChangeVersion = item.ChangeVersion.Version;
        var changeKind = ChangeKind.Update;

        if (item.IsTombstone) {
          changeKind = ChangeKind.Deleted;
          lastChangeVersion = item.TombstoneVersion.Version;
        }

        if (mappedKnowledge.Contains(replicaState.Id, item.SyncId, lastChangeVersion)) {
          keyTracker.UnrequestKeySync(item.SyncTargetKey);
          continue;
        }

        var change = new ItemChange(metadataManager.IdFormats, replicaState.Id, item.SyncId, changeKind, createdVersion, lastChangeVersion);
        var changeData = new ItemChangeData {
          Change = change,
          Identity = new Identity(item.SyncTargetKey, item.SyncId),
        };

        if (!item.IsTombstone) {
          keyTracker.RegisterKeySync(item.SyncTargetKey);
          var syncTarget = item.SyncTarget;
          var entityTuple = entityAccessor.GetEntityState(syncTarget).Tuple;
          changeData.TupleValue = tupleFormatters.Get(syncTarget.TypeInfo.UnderlyingType).Format(entityTuple);
          var type = item.SyncTargetKey.TypeInfo;
          var fields = type.Fields.Where(f => f.IsEntity);
          foreach (var field in fields) {
            var key = entityAccessor.GetReferenceKey(syncTarget, field);
            if (key!=null) {
              changeData.References.Add(field.Name, new Identity(key));
              references.Add(key);
              entityAccessor.SetReferenceKey(syncTarget, field, null);
            }
          }
        }

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

    private void LoadReferences(IEnumerable<ItemChangeData> items, IEnumerable<Key> keys)
    {
      var metadataSet = metadataManager.GetMetadata(keys.ToList());

      foreach (var item in items)
        foreach (var reference in item.References.Values) {
          var metadata = metadataSet[reference.Key];
          if (metadata==null)
            continue;
          reference.Key = metadata.SyncTargetKey;
          reference.GlobalId = metadata.SyncId;
          keyTracker.RequestKeySync(metadata.SyncTargetKey);
        }
    }

    public ChangeDetector(
      ReplicaState replicaState, SyncConfiguration configuration,
      MetadataManager metadataManager, DirectEntityAccessor entityAccessor,
      EntityTupleFormatterRegistry tupleFormatters)
    {
      if (replicaState==null)
        throw new ArgumentNullException("replicaState");
      if (metadataManager==null)
        throw new ArgumentNullException("metadataManager");
      if (configuration==null)
        throw new ArgumentNullException("configuration");
      if (entityAccessor==null)
        throw new ArgumentNullException("entityAccessor");
      if (tupleFormatters==null)
        throw new ArgumentNullException("tupleFormatters");

      this.replicaState = replicaState;
      this.metadataManager = metadataManager;
      this.configuration = configuration;
      this.entityAccessor = entityAccessor;
      this.tupleFormatters = tupleFormatters;

      keyTracker = new KeyTracker(configuration);
    }
  }
}