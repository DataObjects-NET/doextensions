using System;
using Microsoft.Synchronization;
using Xtensive.Core;
using Xtensive.Orm.Services;
using Xtensive.Orm.Sync.DataExchange;

namespace Xtensive.Orm.Sync
{
  internal sealed class SyncSession
  {
    private readonly Session session;
    private readonly SyncSessionContext syncContext;
    private readonly SyncConfiguration configuration;
    private readonly MetadataManager metadataManager;

    private ChangeBatchBuilder batchBuilder;
    private ChangeApplier changeApplier;

    public ReplicaState ReplicaState { get { return metadataManager.ReplicaState; } }

    public Tuple<ChangeBatch, ChangeSet> GetChangeBatch(uint batchSize, SyncKnowledge destinationKnowledge)
    {
      if (batchBuilder==null)
        batchBuilder = new ChangeBatchBuilder(metadataManager, configuration);

      var result = batchBuilder.GetNextBatch(batchSize, destinationKnowledge);

      if (batchBuilder.IsCompleted)
        batchBuilder = null;

      return result;
    }

    public void ProcessChangeBatch(ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges, IChangeDataRetriever changeDataRetriever, SyncCallbacks syncCallbacks)
    {
      if (changeApplier==null)
        changeApplier = CreateChangerApplier();

      changeApplier.ProcessChangeBatch(resolutionPolicy, sourceChanges, changeDataRetriever, syncCallbacks, syncContext);
    }

    private ChangeApplier CreateChangerApplier()
    {
      var accessor = session.Services.Demand<DirectEntityAccessor>();
      var metadataFetcher = session.Services.Demand<MetadataFetcher>();
      var tickGenerator = session.Services.Demand<SyncTickGenerator>();
      var tupleFormatters = session.Services.Demand<EntityTupleFormatterRegistry>();

      return new ChangeApplier(metadataManager, metadataFetcher, accessor, tickGenerator, tupleFormatters);
    }

    public void Complete()
    {
      batchBuilder = null;
      changeApplier = null;

      metadataManager.SaveReplicaState();
    }

    public SyncSession(SyncSessionContext syncContext, Session session, SyncConfiguration configuration)
    {
      if (syncContext==null)
        throw new ArgumentNullException("syncContext");
      if (session==null)
        throw new ArgumentNullException("session");
      if (configuration==null)
        throw new ArgumentNullException("configuration");

      this.syncContext = syncContext;
      this.session = session;
      this.configuration = configuration;

      metadataManager = session.Services.Demand<MetadataManager>();
      metadataManager.Configure(configuration);
      metadataManager.LoadReplicaState();
    }
  }
}