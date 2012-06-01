using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Synchronization;
using Xtensive.IoC;
using Xtensive.Orm.Services;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (SyncProviderImplementation))]
  public class SyncProviderImplementation : KnowledgeSyncProvider,
    IChangeDataRetriever,
    INotifyingChangeApplierTarget,
    ISessionService
  {
    private readonly Session session;
    private readonly SyncMetadataStore metadataStore;

    private SyncSessionContext syncContext;
    private int totalItemCount;
    private int currentItemCount;
    IEnumerator<ItemChange> enumerator;

    public override SyncIdFormatGroup IdFormats
    {
      get { return Wellknown.IdFormats; }
    }

    public override void BeginSession(SyncProviderPosition position, SyncSessionContext syncSessionContext)
    {
      syncContext = syncSessionContext;
      totalItemCount = session.Query.All<ISyncInfo>().Count();
      currentItemCount = 0;
    }

    public override void EndSession(SyncSessionContext syncSessionContext)
    {
    }

    public override void GetSyncBatchParameters(out uint batchSize, out SyncKnowledge knowledge)
    {
      batchSize = Wellknown.SyncBatchSize;
      knowledge = metadataStore.CurrentKnowledge;

      if (totalItemCount < Wellknown.SyncBatchSize)
        batchSize = (uint) totalItemCount;
    }

    public override ChangeBatch GetChangeBatch(uint batchSize, SyncKnowledge destinationKnowledge,
      out object changeDataRetriever)
    {
      changeDataRetriever = this;
      return DetectChanges(batchSize, destinationKnowledge);
    }

    private ChangeBatch DetectChanges(uint batchSize, SyncKnowledge destinationKnowledge)
    {
      var changeBatch = new ChangeBatch(IdFormats, destinationKnowledge, metadataStore.ForgottenKnowledge);

      if (currentItemCount==0) {
        var items = metadataStore.DetectChanges(destinationKnowledge);
        enumerator = items.GetEnumerator();
        changeBatch.BeginUnorderedGroup();
      }
      else
        changeBatch.BeginUnorderedGroup();
      
      uint batchItemCount = 0;
      while (batchItemCount < batchSize && enumerator.MoveNext()) {
        batchItemCount++;
        changeBatch.AddChange(enumerator.Current);
        currentItemCount++;
      }
      
      if (currentItemCount!=totalItemCount)
        changeBatch.EndUnorderedGroup(metadataStore.CurrentKnowledge, false);
      else {
        changeBatch.SetLastBatch();
        changeBatch.EndUnorderedGroup(metadataStore.CurrentKnowledge, true);
        enumerator.Dispose();
      }

      return changeBatch;
    }

    public override void ProcessChangeBatch(ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges,
      object changeDataRetriever, SyncCallbacks syncCallbacks, SyncSessionStatistics sessionStatistics)
    {
      var localChanges = metadataStore.GetLocalChanges(sourceChanges);
      var changeApplier = new NotifyingChangeApplier(IdFormats);

      changeApplier.ApplyChanges(resolutionPolicy, sourceChanges, changeDataRetriever as IChangeDataRetriever,
        localChanges, metadataStore.CurrentKnowledge.Clone(), metadataStore.ForgottenKnowledge, this, syncContext, syncCallbacks);
    }

    public IChangeDataRetriever GetDataRetriever()
    {
      return this;
    }

    public ulong GetNextTickCount()
    {
      return (ulong) metadataStore.NextTick;
    }

    public object LoadChangeData(LoadChangeContext loadChangeContext)
    {
      var id = loadChangeContext.ItemChange.ItemId.GetGuidId();
      var info = session.Query.All<SyncInfo>().SingleOrDefault(i => i.GlobalId == id);
      var accessor = session.Services.Get<DirectPersistentAccessor>();
      var syncInfoMetadata = metadataStore.GetSyncInfoMetadata(info.GetType().GetGenericArguments()[0]);
      throw new NotImplementedException();
    }

    public void SaveConflict(ItemChange conflictingChange, object conflictingChangeData, SyncKnowledge conflictingChangeKnowledge)
    {
      throw new NotImplementedException();
    }

    public void SaveItemChange(SaveChangeAction saveChangeAction, ItemChange change, SaveChangeContext context)
    {
      SyncInfo syncInfo;
      var source = (SyncInfo)context.ChangeData;

      switch (saveChangeAction) {
        case SaveChangeAction.Create:
          var accessor = session.Services.Get<DirectPersistentAccessor>();
          var syncInfoMetadata = metadataStore.GetSyncInfoMetadata(source.GetType().GetGenericArguments()[0]);
          var entityKey = accessor.GetReferenceKey(source, syncInfoMetadata.EntityField);
          syncInfo = (SyncInfo) accessor.CreateEntity(syncInfoMetadata.UnderlyingType);
          accessor.SetReferenceKey(syncInfo, syncInfoMetadata.EntityField, entityKey);

          syncInfo.GlobalId = change.ItemId.GetGuidId();
          syncInfo.CreatedVersion = change.CreationVersion;
          syncInfo.ChangeVersion = change.ChangeVersion;
          break;
      }
    }

    public void StoreKnowledgeForScope(SyncKnowledge newCurrentKnowledge, ForgottenKnowledge newForgottenKnowledge)
    {
      metadataStore.UpdateKnowledge(newCurrentKnowledge, newForgottenKnowledge);
    }

    public bool TryGetDestinationVersion(ItemChange sourceChange, out ItemChange destinationVersion)
    {
      throw new NotImplementedException();
    }

    #region Not supported methods

    public override FullEnumerationChangeBatch GetFullEnumerationChangeBatch(uint batchSize, SyncId lowerEnumerationBound, SyncKnowledge knowledgeForDataRetrieval, out object changeDataRetriever)
    {
      throw new NotSupportedException();
    }

    public override void ProcessFullEnumerationChangeBatch(ConflictResolutionPolicy resolutionPolicy, FullEnumerationChangeBatch sourceChanges, object changeDataRetriever, SyncCallbacks syncCallbacks, SyncSessionStatistics sessionStatistics)
    {
      throw new NotSupportedException();
    }

    public void SaveChangeWithChangeUnits(ItemChange change, SaveChangeWithChangeUnitsContext context)
    {
      throw new NotSupportedException();
    }

    #endregion

    [ServiceConstructor]
    public SyncProviderImplementation(Session session)
    {
      this.session = session;
      metadataStore = session.Services.Get<SyncMetadataStore>();
    }
  }
}