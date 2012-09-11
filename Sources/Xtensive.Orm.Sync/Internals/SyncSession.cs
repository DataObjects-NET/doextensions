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
    private readonly ReplicaManager replicaManager;

    private ChangeBatchBuilder batchBuilder;
    private ChangeApplier changeApplier;

    public ReplicaState ReplicaState { get; private set; }

    public Tuple<ChangeBatch, ChangeSet> GetChangeBatch(uint batchSize, SyncKnowledge destinationKnowledge)
    {
      if (batchBuilder==null)
        batchBuilder = CreateBatchBuilder();

      return batchBuilder.GetNextBatch(batchSize, destinationKnowledge);
    }

    public void ProcessChangeBatch(ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges, IChangeDataRetriever changeDataRetriever, SyncCallbacks syncCallbacks)
    {
      if (changeApplier==null)
        changeApplier = CreateChangerApplier();

      changeApplier.ProcessChangeBatch(resolutionPolicy, sourceChanges, changeDataRetriever, syncCallbacks, syncContext);
    }

    private ChangeBatchBuilder CreateBatchBuilder()
    {
      var metadataManager = session.Services.Demand<MetadataManager>();
      var accessor = session.Services.Demand<DirectEntityAccessor>();
      var tupleFormatters = session.Services.Demand<EntityTupleFormatterRegistry>();
      var changeDetector = new ChangeDetector(ReplicaState, configuration, metadataManager, accessor, tupleFormatters);

      return new ChangeBatchBuilder(ReplicaState, configuration, metadataManager, changeDetector);
    }

    private ChangeApplier CreateChangerApplier()
    {
      var metadataManager = session.Services.Demand<MetadataManager>();
      var metadataFetcher = session.Services.Demand<MetadataFetcher>();
      var accessor = session.Services.Demand<DirectEntityAccessor>();
      var tickGenerator = session.Services.Demand<SyncTickGenerator>();
      var tupleFormatters = session.Services.Demand<EntityTupleFormatterRegistry>();

      return new ChangeApplier(ReplicaState, metadataManager, metadataFetcher, accessor, tickGenerator, tupleFormatters);
    }

    public void Complete()
    {
      batchBuilder = null;
      changeApplier = null;

      replicaManager.SaveReplicaState(ReplicaState);
    }

    public SyncSession(SyncSessionContext syncContext, Session session, SyncConfiguration configuration)
    {
      if (session==null)
        throw new ArgumentNullException("session");
      if (configuration==null)
        throw new ArgumentNullException("configuration");

      this.syncContext = syncContext;
      this.session = session;
      this.configuration = configuration;

      replicaManager = session.Services.Demand<ReplicaManager>();
      ReplicaState = replicaManager.LoadReplicaState();
    }
  }
}