using System;
using System.Collections.Generic;
using Microsoft.Synchronization;
using Xtensive.Orm.Sync.DataExchange;

namespace Xtensive.Orm.Sync
{
  internal sealed class ChangeBatchBuilder
  {
    private readonly SyncConfiguration configuration;
    private readonly ChangeDetector changeDetector;
    private readonly ReplicaState replicaState;
    private readonly MetadataManager metadataManager;

    private IEnumerator<ChangeSet> changeSetIterator;

    public Tuple<ChangeBatch, ChangeSet> GetNextBatch(uint batchSize, SyncKnowledge destinationKnowledge)
    {
      var batch = CreateChangeBatch(destinationKnowledge);

      if (changeSetIterator==null) {
        var changeSets = changeDetector.DetectChanges(batchSize, destinationKnowledge);
        changeSetIterator = changeSets.GetEnumerator();
        if (!changeSetIterator.MoveNext()) {
          batch.BeginUnorderedGroup();
          batch.EndUnorderedGroup(replicaState.CurrentKnowledge, true);
          CompleteWork(batch);
          return MakeResult(batch, new ChangeSet());
        }
      }

      var changeSet = changeSetIterator.Current;
      var isLastBatch = !changeSetIterator.MoveNext();
      AddChanges(batch, changeSet, isLastBatch);
      return MakeResult(batch, changeSet);
    }

    private void CompleteWork(ChangeBatch lastBatch)
    {
      lastBatch.SetLastBatch();
      changeSetIterator.Dispose();
    }

    private void AddChanges(ChangeBatch batch, ChangeSet changeSet, bool isLastBatch)
    {
      if (changeSet.IsRange) {
        batch.BeginOrderedGroup(changeSet.MinId);
        batch.AddChanges(changeSet.GetItemChanges());
        batch.EndOrderedGroup(changeSet.MaxId, replicaState.CurrentKnowledge);
      }
      else {
        batch.BeginUnorderedGroup();
        batch.AddChanges(changeSet.GetItemChanges());
        batch.EndUnorderedGroup(replicaState.CurrentKnowledge, isLastBatch);
      }

      if (isLastBatch)
        CompleteWork(batch);
    }

    private Tuple<ChangeBatch, ChangeSet> MakeResult(ChangeBatch batch, ChangeSet changeSet)
    {
      return new Tuple<ChangeBatch, ChangeSet>(batch, changeSet);
    }

    private ChangeBatch CreateChangeBatch(SyncKnowledge destinationKnowledge)
    {
      var idFormats = metadataManager.IdFormats;
      if (!FilteredBatchIsRequired())
        return new ChangeBatch(idFormats, destinationKnowledge, replicaState.ForgottenKnowledge);
      var filterInfo = new ItemListFilterInfo(idFormats);
      return new ChangeBatch(idFormats, destinationKnowledge, replicaState.ForgottenKnowledge, filterInfo);
    }

    private bool FilteredBatchIsRequired()
    {
      return configuration.SyncTypes.Count > 0
        || configuration.Filters.Count > 0
        || configuration.SkipTypes.Count > 0;
    }

    public ChangeBatchBuilder(ReplicaState replicaState, SyncConfiguration configuration, MetadataManager metadataManager, ChangeDetector changeDetector)
    {
      if (replicaState==null)
        throw new ArgumentNullException("replicaState");
      if (metadataManager==null)
        throw new ArgumentNullException("metadataManager");
      if (configuration==null)
        throw new ArgumentNullException("configuration");
      if (changeDetector==null)
        throw new ArgumentNullException("changeDetector");

      this.replicaState = replicaState;
      this.metadataManager = metadataManager;
      this.configuration = configuration;
      this.changeDetector = changeDetector;
    }
  }
}