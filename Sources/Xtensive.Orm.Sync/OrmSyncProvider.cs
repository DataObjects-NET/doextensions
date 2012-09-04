using System;
#if DEBUG
using System.Diagnostics;
#endif
using Microsoft.Synchronization;
using Xtensive.IoC;
using Xtensive.Orm.Tracking;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// <see cref="KnowledgeSyncProvider"/> wrapper for <see cref="Xtensive.Orm"/>.
  /// </summary>
  [Service(typeof (OrmSyncProvider))]
  public sealed class OrmSyncProvider : KnowledgeSyncProvider,
    INotifyingChangeApplierTarget,
    IDomainService
  {
    private readonly Domain domain;
    private readonly SyncConfiguration configuration;

    private SyncProviderImplementation implementation;
    private Session session;
    private SyncSessionContext syncContext;
    private TransactionScope transaction;
    private IDisposable persistLock;

#if DEBUG
    private int batchCounter;
    private Stopwatch batchStopwatch;
    private Stopwatch sessionStopwatch;
#endif

    private bool IsRunning { get { return session!=null; } }

    /// <summary>
    /// Endpoint for fluent configuration of this instance.
    /// </summary>
    public SyncConfigurationEndpoint Sync { get; private set; }

    #region KnowledgeSyncProvider Members

    /// <summary>
    /// When overridden in a derived class, gets the ID format schema of the provider.
    /// </summary>
    /// <returns>The ID format schema of the provider.</returns>
    public override SyncIdFormatGroup IdFormats { get { return Wellknown.IdFormats; } }

    /// <summary>
    /// When overridden in a derived class, notifies the provider that it is joining a synchronization session.
    /// </summary>
    /// <param name="position">The position of this provider, relative to the other provider in the session.</param>
    /// <param name="syncSessionContext">The current status of the corresponding session.</param>
    public override void BeginSession(SyncProviderPosition position, SyncSessionContext syncSessionContext)
    {
      if (IsRunning)
        throw new InvalidOperationException("Sync is already running");

#if DEBUG
      batchCounter = 0;
      batchStopwatch = new Stopwatch();
      sessionStopwatch = new Stopwatch();
      sessionStopwatch.Start();
      Debug.WriteLine("Starting synchronization session @ {0}", DateTime.Now);
#endif

      syncContext = syncSessionContext;
      session = domain.OpenSession();
      var trackingMonitor = session.Services.Get<ISessionTrackingMonitor>();
      if (trackingMonitor!=null)
        trackingMonitor.Disable();
      transaction = session.OpenTransaction();
      persistLock = session.DisableSaveChanges();
      if (configuration.SyncTypes.Count==0 && configuration.Filters.Count==0 && configuration.SkipTypes.Count==0)
        configuration.SyncAll = true;
      implementation = new SyncProviderImplementation(session, configuration);
    }

    /// <summary>
    /// When overridden in a derived class, notifies the provider that a synchronization session to which it was enlisted has completed.
    /// </summary>
    /// <param name="syncSessionContext">The current status of the corresponding session.</param>
    public override void EndSession(SyncSessionContext syncSessionContext)
    {
      if (!IsRunning)
        return;

      try {
        implementation.Replica.UpdateState();
#if DEBUG
        sessionStopwatch.Stop();
        Debug.WriteLine("Finishing synchronization session. Elapsed time: {0}", sessionStopwatch.Elapsed);
        sessionStopwatch = null;
        batchStopwatch.Stop();
        batchStopwatch = null;
#endif
        persistLock.Dispose();
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

    /// <summary>
    /// When overridden in a derived class, gets a change batch that contains item metadata for items that are not contained in the specified knowledge from the destination provider.
    /// </summary>
    /// <param name="batchSize">The number of changes to include in the change batch.</param>
    /// <param name="destinationKnowledge">The knowledge from the destination provider. This knowledge must be mapped by calling <see cref="M:Microsoft.Synchronization.SyncKnowledge.MapRemoteKnowledgeToLocal(Microsoft.Synchronization.SyncKnowledge)"/> on the source knowledge before it can be used for change enumeration.</param>
    /// <param name="changeDataRetriever">Returns an object that can be used to retrieve change data. It can be an <see cref="T:Microsoft.Synchronization.IChangeDataRetriever"/> object or a provider-specific object.</param>
    /// <returns>
    /// A change batch that contains item metadata for items that are not contained in the specified knowledge from the destination provider. Cannot be a null.
    /// </returns>
    public override ChangeBatch GetChangeBatch(uint batchSize, SyncKnowledge destinationKnowledge,
      out object changeDataRetriever)
    {
#if DEBUG
      ++batchCounter;
      batchStopwatch.Restart();
#endif

      CheckIsRunning();
      var result = implementation.GetChangeBatch(batchSize, destinationKnowledge);
      changeDataRetriever = (this as INotifyingChangeApplierTarget).GetDataRetriever();

#if DEBUG
      Debug.WriteLine("GetChangeBatch #{0}, {1} ms", batchCounter, batchStopwatch.ElapsedMilliseconds);
#endif
      return result;
    }

    /// <summary>
    /// When overridden in a derived class, gets the number of item changes that will be included in change batches, and the current knowledge for the synchronization scope.
    /// </summary>
    /// <param name="batchSize">The number of item changes that will be included in change batches returned by this object.</param>
    /// <param name="knowledge">The current knowledge for the synchronization scope, or a newly created knowledge object if no current knowledge exists.</param>
    public override void GetSyncBatchParameters(out uint batchSize, out SyncKnowledge knowledge)
    {
      CheckIsRunning();
      batchSize = (uint) configuration.BatchSize;
      knowledge = implementation.Replica.CurrentKnowledge;
    }

    /// <summary>
    /// When overridden in a derived class, processes a set of changes by detecting conflicts and applying changes to the item store.
    /// </summary>
    /// <param name="resolutionPolicy">The conflict resolution policy to use when this method applies changes.</param>
    /// <param name="sourceChanges">A batch of changes from the source provider to be applied locally.</param>
    /// <param name="changeDataRetriever">An object that can be used to retrieve change data. It can be an <see cref="T:Microsoft.Synchronization.IChangeDataRetriever"/> object or a provider-specific object.</param>
    /// <param name="syncCallbacks">An object that receives event notifications during change application.</param>
    /// <param name="sessionStatistics">Tracks change statistics. For a provider that uses custom change application, this object must be updated with the results of the change application.</param>
    public override void ProcessChangeBatch(ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges,
      object changeDataRetriever, SyncCallbacks syncCallbacks, SyncSessionStatistics sessionStatistics)
    {
#if DEBUG
      ++batchCounter;
      batchStopwatch.Restart();
#endif

      CheckIsRunning();
      implementation.ProcessChangeBatch(resolutionPolicy, sourceChanges, changeDataRetriever, syncCallbacks, sessionStatistics, syncContext, this);

#if DEBUG
      Debug.WriteLine("ProcessChangeBatch #{0}, {1} ms", batchCounter, batchStopwatch.ElapsedMilliseconds);
#endif
    }

    /// <summary>
    /// When overridden in a derived class, gets a change batch that contains item metadata for items that have IDs greater than the specified lower bound, as part of a full enumeration.
    /// </summary>
    /// <param name="batchSize">The number of changes to include in the change batch.</param>
    /// <param name="lowerEnumerationBound">The lower bound for item IDs. This method returns changes that have IDs greater than or equal to this ID value.</param>
    /// <param name="knowledgeForDataRetrieval">If an item change is contained in this knowledge object, data for that item already exists on the destination replica.</param>
    /// <param name="changeDataRetriever">Returns an object that can be used to retrieve change data. It can be an <see cref="T:Microsoft.Synchronization.IChangeDataRetriever"/> object or a be provider-specific object.</param>
    /// <returns>
    /// A change batch that contains item metadata for items that have IDs greater than the specified lower bound, as part of a full enumeration.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Thrown always</exception>
    public override FullEnumerationChangeBatch GetFullEnumerationChangeBatch(uint batchSize, SyncId lowerEnumerationBound, SyncKnowledge knowledgeForDataRetrieval, out object changeDataRetriever)
    {
      throw new NotSupportedException();
    }

    /// <summary>
    /// When overridden in a derived class, processes a set of changes for a full enumeration by applying changes to the item store.
    /// </summary>
    /// <param name="resolutionPolicy">The conflict resolution policy to use when this method applies changes.</param>
    /// <param name="sourceChanges">A batch of changes from the source provider to be applied locally.</param>
    /// <param name="changeDataRetriever">An object that can be used to retrieve change data. It can be an <see cref="T:Microsoft.Synchronization.IChangeDataRetriever"/> object or a provider-specific object.</param>
    /// <param name="syncCallbacks">An object that receives event notifications during change application.</param>
    /// <param name="sessionStatistics">Tracks change statistics. For a provider that uses custom change application, this object must be updated with the results of the change application.</param>
    /// <exception cref="System.NotSupportedException">Thrown always</exception>
    public override void ProcessFullEnumerationChangeBatch(ConflictResolutionPolicy resolutionPolicy, FullEnumerationChangeBatch sourceChanges, object changeDataRetriever, SyncCallbacks syncCallbacks, SyncSessionStatistics sessionStatistics)
    {
      throw new NotSupportedException();
    }

    #endregion

    #region INotifyingChangeApplierTarget Members

    /// <summary>
    /// Gets an object that can be used to retrieve item data from a replica.
    /// </summary>
    /// <returns>
    /// An object that can be used to retrieve item data from a replica.
    /// </returns>
    IChangeDataRetriever INotifyingChangeApplierTarget.GetDataRetriever()
    {
      return new ChangeDataRetriever(IdFormats, implementation.CurrentChangeSet);
    }

    /// <summary>
    /// When overridden in a derived class, increments the tick count and returns the new tick count.
    /// </summary>
    /// <returns>
    /// The newly incremented tick count.
    /// </returns>
    ulong INotifyingChangeApplierTarget.GetNextTickCount()
    {
      return implementation.GetNextTickCount();
    }

    /// <summary>
    /// When overridden in a derived class, saves an item change that contains unit change changes to the item store.
    /// </summary>
    /// <param name="change">The item change to apply.</param>
    /// <param name="context">Information about the change to be applied.</param>
    void INotifyingChangeApplierTarget.SaveChangeWithChangeUnits(ItemChange change, SaveChangeWithChangeUnitsContext context)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// When overridden in a derived class, saves information about a change that caused a conflict.
    /// </summary>
    /// <param name="conflictingChange">The item metadata for the conflicting change.</param>
    /// <param name="conflictingChangeData">The item data for the conflicting change.</param>
    /// <param name="conflictingChangeKnowledge">The knowledge to be learned if this change is applied. This must be saved with the change.</param>
    void INotifyingChangeApplierTarget.SaveConflict(ItemChange conflictingChange, object conflictingChangeData, SyncKnowledge conflictingChangeKnowledge)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// When overridden in a derived class, saves an item change to the item store.
    /// </summary>
    /// <param name="saveChangeAction">The action to be performed for the change.</param>
    /// <param name="change">The item change to save.</param>
    /// <param name="context">Information about the change to be applied.</param>
    void INotifyingChangeApplierTarget.SaveItemChange(SaveChangeAction saveChangeAction, ItemChange change, SaveChangeContext context)
    {
      implementation.SaveItemChange(saveChangeAction, change, context);
    }

    /// <summary>
    /// When overridden in a derived class, stores the knowledge for the current scope.
    /// </summary>
    /// <param name="knowledge">The updated knowledge to be saved.</param>
    /// <param name="forgottenKnowledge">The forgotten knowledge to be saved.</param>
    void INotifyingChangeApplierTarget.StoreKnowledgeForScope(SyncKnowledge knowledge, ForgottenKnowledge forgottenKnowledge)
    {
      implementation.StoreKnowledgeForScope(knowledge, forgottenKnowledge);
    }

    /// <summary>
    /// Gets the version of an item stored in the destination replica.
    /// </summary>
    /// <returns>
    /// true if the item was found in the destination replica; otherwise, false. 
    /// </returns>
    /// <param name="sourceChange">The item change that is sent by the source provider.</param>
    /// <param name="destinationVersion">Returns an item change that contains the version of the item in the destination replica.</param>
    bool INotifyingChangeApplierTarget.TryGetDestinationVersion(ItemChange sourceChange, out ItemChange destinationVersion)
    {
      throw new NotImplementedException();
    }

    #endregion

    private void CheckIsRunning()
    {
      if (!IsRunning)
        throw new InvalidOperationException("Sync session is not active");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrmSyncProvider"/> class.
    /// </summary>
    /// <param name="domain">The domain.</param>
    [ServiceConstructor]
    public OrmSyncProvider(Domain domain)
    {
      this.domain = domain;
      configuration = new SyncConfiguration();
      Sync = configuration.Endpoint;
    }
  }
}