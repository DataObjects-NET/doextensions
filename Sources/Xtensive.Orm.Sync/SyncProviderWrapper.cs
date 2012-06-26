using System;
using Microsoft.Synchronization;
using Xtensive.IoC;
using Xtensive.Orm.Tracking;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// <see cref="KnowledgeSyncProvider"/> wrapper.
  /// </summary>
  [Service(typeof (SyncProviderWrapper), Singleton = true)]
  public class SyncProviderWrapper : KnowledgeSyncProvider,
    IDomainService
  {
    private readonly Domain domain;
    private Session session;
    private TransactionScope transaction;
    private SyncProviderImplementation implementation;
    private IDisposable persistLock;

    private bool IsRunning
    {
      get { return session!=null; }
    }

    /// <summary>
    /// When overridden in a derived class, gets the ID format schema of the provider.
    /// </summary>
    /// <returns>The ID format schema of the provider.</returns>
    public override SyncIdFormatGroup IdFormats
    {
      get { return Wellknown.IdFormats; }
    }

    /// <summary>
    /// Gets the configuration settings for the provider.
    /// </summary>
    /// <returns>The configuration settings for the provider.</returns>
    public new SyncConfiguration Configuration { get; set; }

    /// <summary>
    /// When overridden in a derived class, notifies the provider that it is joining a synchronization session.
    /// </summary>
    /// <param name="position">The position of this provider, relative to the other provider in the session.</param>
    /// <param name="syncSessionContext">The current status of the corresponding session.</param>
    public override void BeginSession(SyncProviderPosition position, SyncSessionContext syncSessionContext)
    {
      if (IsRunning)
        throw new InvalidOperationException("Sync is already running");

      session = domain.OpenSession();
      var tm = session.Services.Get<ISessionTrackingMonitor>();
      if (tm != null)
        tm.Disable();
      transaction = session.OpenTransaction();
      persistLock = session.DisableSaveChanges();
      implementation = new SyncProviderImplementation(session, Configuration);
      implementation.BeginSession(position, syncSessionContext);
    }

    /// <summary>
    /// When overridden in a derived class, notifies the provider that a synchronization session to which it was enlisted has completed.
    /// </summary>
    /// <param name="syncSessionContext">The current status of the corresponding session.</param>
    public override void EndSession(SyncSessionContext syncSessionContext)
    {
      if (!IsRunning)
        return;

      implementation.EndSession(syncSessionContext);

      try {
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
    /// When overridden in a derived class, gets the number of item changes that will be included in change batches, and the current knowledge for the synchronization scope.
    /// </summary>
    /// <param name="batchSize">The number of item changes that will be included in change batches returned by this object.</param>
    /// <param name="knowledge">The current knowledge for the synchronization scope, or a newly created knowledge object if no current knowledge exists.</param>
    public override void GetSyncBatchParameters(out uint batchSize, out SyncKnowledge knowledge)
    {
      CheckIsRunning();
      implementation.GetSyncBatchParameters(out batchSize, out knowledge);
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
      CheckIsRunning();
      return implementation.GetChangeBatch(batchSize, destinationKnowledge, out changeDataRetriever);
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
      CheckIsRunning();
      implementation.ProcessChangeBatch(resolutionPolicy, sourceChanges, changeDataRetriever, syncCallbacks, sessionStatistics);
    }

    #region Not supported methods

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
      Configuration = new SyncConfiguration();
    }
  }
}