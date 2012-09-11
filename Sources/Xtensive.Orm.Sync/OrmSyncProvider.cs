using System;
using System.Diagnostics;
using Microsoft.Synchronization;
using Xtensive.Core;
using Xtensive.IoC;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Sync.DataExchange;
using Xtensive.Orm.Tracking;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// <see cref="KnowledgeSyncProvider"/> wrapper for <see cref="Xtensive.Orm"/>.
  /// </summary>
  [Service(typeof (OrmSyncProvider), Singleton = false)]
  public sealed class OrmSyncProvider : KnowledgeSyncProvider, IDomainService
  {
    private static readonly SessionConfiguration OrmSessionConfiguration;

    private readonly Domain domain;
    private readonly SyncConfiguration configuration;

    private TransactionScope transaction;
    private IDisposable sessionResources;

    private SyncSession syncSession;

#if DEBUG
    private int batchCounter;
    private Stopwatch batchStopwatch;
    private Stopwatch sessionStopwatch;
#endif

    private bool IsRunning { get { return syncSession!=null; } }

    /// <summary>
    /// Endpoint for fluent configuration of this instance.
    /// </summary>
    public SyncConfigurationEndpoint Sync { get; private set; }

    /// <summary>
    /// Gets <see cref="SyncConfiguration"/> for this instance.
    /// </summary>
    public SyncConfiguration SyncConfiguration { get { return configuration; } }

    #region KnowledgeSyncProvider members

    /// <summary>
    /// When overridden in a derived class, gets the ID format schema of the provider.
    /// </summary>
    /// <returns>The ID format schema of the provider.</returns>
    public override SyncIdFormatGroup IdFormats { get { return WellKnown.IdFormats; } }

    /// <summary>
    /// When overridden in a derived class, notifies the provider that it is joining a synchronization session.
    /// </summary>
    /// <param name="position">The position of this provider, relative to the other provider in the session.</param>
    /// <param name="syncSessionContext">The current status of the corresponding session.</param>
    public override void BeginSession(SyncProviderPosition position, SyncSessionContext syncSessionContext)
    {
      if (IsRunning)
        throw new InvalidOperationException("Sync is already running");

      BeginSessionTracing();

      var resources = new DisposableSet();

      try {
        Session session;

        // Prepare session
        if (configuration.Session==null) {
          session = domain.OpenSession(OrmSessionConfiguration);
          resources.Add(session);
        }
        else {
          session = configuration.Session;
          if (session.Configuration.Options.HasFlag(SessionOptions.Disconnected))
            throw new NotSupportedException("Disconnected sessions are not supported for synchronization");
        }

        // Prepare tracking monitor
        var trackingMonitor = session.Services.Get<ISessionTrackingMonitor>();
        if (trackingMonitor!=null)
          trackingMonitor.Disable();

        // Prepare transaction
        transaction = session.OpenTransaction();
        resources.Add(transaction);

        // Disable persisting
        resources.Add(session.DisableSaveChanges());

        // Prepare configuration
        configuration.Prepare();

        syncSession = new SyncSession(syncSessionContext, session, configuration);
      }
      catch {
        ((IDisposable) resources).Dispose();
        throw;
      }

      sessionResources = resources;
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
        syncSession.Complete();
        transaction.Complete();
        sessionResources.Dispose();
        EndSessionTracing();
      }
      finally {
        syncSession = null;
        transaction = null;
        sessionResources = null;
      }
    }

    [Conditional("DEBUG")]
    private void BeginSessionTracing()
    {
#if DEBUG
      batchCounter = 0;
      batchStopwatch = new Stopwatch();
      sessionStopwatch = new Stopwatch();
      sessionStopwatch.Start();
      Debug.WriteLine("Starting synchronization session @ {0}", DateTime.Now);
#endif
    }

    [Conditional("DEBUG")]
    private void EndSessionTracing()
    {
#if DEBUG
      sessionStopwatch.Stop();
      Debug.WriteLine("Finishing synchronization session. Elapsed time: {0}", sessionStopwatch.Elapsed);
      sessionStopwatch = null;
      batchStopwatch.Stop();
      batchStopwatch = null;
#endif
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
    public override ChangeBatch GetChangeBatch(
      uint batchSize, SyncKnowledge destinationKnowledge, out object changeDataRetriever)
    {
#if DEBUG
      ++batchCounter;
      batchStopwatch.Restart();
#endif

      CheckIsRunning();
      var result = syncSession.GetChangeBatch(batchSize, destinationKnowledge);
      changeDataRetriever = new ChangeDataRetriever(IdFormats, result.Item2);

#if DEBUG
      Debug.WriteLine("GetChangeBatch #{0}, {1} ms", batchCounter, batchStopwatch.ElapsedMilliseconds);
#endif
      return result.Item1;
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
      knowledge = syncSession.ReplicaState.CurrentKnowledge;
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
      syncSession.ProcessChangeBatch(resolutionPolicy, sourceChanges, (IChangeDataRetriever) changeDataRetriever, syncCallbacks);

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
      throw new NotSupportedException("GetFullEnumerationChangeBatch");
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
      throw new NotSupportedException("ProcessFullEnumerationChangeBatch");
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
      Sync = new SyncConfigurationEndpoint(configuration);
    }

    static OrmSyncProvider()
    {
      OrmSessionConfiguration = new SessionConfiguration("Sync", SessionOptions.ServerProfile);
      OrmSessionConfiguration.Lock();
    }
  }
}