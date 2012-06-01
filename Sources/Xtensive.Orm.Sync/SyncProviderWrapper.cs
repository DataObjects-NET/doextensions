using System;
using Microsoft.Synchronization;
using Xtensive.IoC;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (SyncProvider), Singleton = true)]
  public class SyncProviderWrapper : KnowledgeSyncProvider,
    IDomainService
  {
    private readonly Domain domain;
    private Session session;
    private TransactionScope transaction;
    private SyncProviderImplementation implementation;
    private SyncIdFormatGroup idFormats;

    private bool IsRunning
    {
      get { return session!=null; }
    }

    public override SyncIdFormatGroup IdFormats
    {
      get { return Wellknown.IdFormats; }
    }

    public override void BeginSession(SyncProviderPosition position, SyncSessionContext syncSessionContext)
    {
      if (IsRunning)
        throw new InvalidOperationException("Sync is already running");

      session = domain.OpenSession();
      transaction = session.OpenTransaction();
      implementation = session.Services.Get<SyncProviderImplementation>();
      implementation.BeginSession(position, syncSessionContext);
    }

    public override void EndSession(SyncSessionContext syncSessionContext)
    {
      if (!IsRunning)
        return;

      implementation.EndSession(syncSessionContext);

      try {
        transaction.Complete();
        transaction.Dispose();
        session.Dispose();
      }
      finally {
        implementation = null;
        transaction = null;
        session = null;        
      }
    }

    public override void GetSyncBatchParameters(out uint batchSize, out SyncKnowledge knowledge)
    {
      CheckIsRunning();
      implementation.GetSyncBatchParameters(out batchSize, out knowledge);
    }

    public override ChangeBatch GetChangeBatch(uint batchSize, SyncKnowledge destinationKnowledge,
      out object changeDataRetriever)
    {
      CheckIsRunning();
      return implementation.GetChangeBatch(batchSize, destinationKnowledge, out changeDataRetriever);
    }

    public override void ProcessChangeBatch(ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges,
      object changeDataRetriever, SyncCallbacks syncCallbacks, SyncSessionStatistics sessionStatistics)
    {
      CheckIsRunning();
      implementation.ProcessChangeBatch(resolutionPolicy, sourceChanges, changeDataRetriever, syncCallbacks, sessionStatistics);
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

    #endregion

    private void CheckIsRunning()
    {
      if (!IsRunning)
        throw new InvalidOperationException("Sync session is not active");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncProviderWrapper"/> class.
    /// </summary>
    /// <param name="domain">The domain.</param>
    [ServiceConstructor]
    public SyncProviderWrapper(Domain domain)
    {
      this.domain = domain;
    }
  }
}