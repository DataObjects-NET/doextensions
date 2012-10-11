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
    private readonly ReplicaInfoManager replicaInfoManager;

    private string activeOperation;

    private ChangeBatchBuilder batchBuilder;
    private ChangeApplier changeApplier;

    public ReplicaInfo ReplicaInfo { get; private set; }

    public Tuple<ChangeBatch, ChangeSet> GetChangeBatch(uint batchSize, SyncKnowledge destinationKnowledge)
    {
      activeOperation = "GetChangeBatch";
      try {
        if (batchBuilder==null)
          batchBuilder = CreateBatchBuilder();
        return batchBuilder.GetNextBatch(batchSize, destinationKnowledge);
      }
      finally {
        activeOperation = null;
      }
    }

    public void ProcessChangeBatch(ConflictResolutionPolicy resolutionPolicy,
      ChangeBatch sourceChanges, IChangeDataRetriever changeDataRetriever, SyncCallbacks syncCallbacks)
    {
      activeOperation = "ProcessChangeBatch";
      try {
        if (changeApplier==null)
          changeApplier = CreateChangerApplier();
        changeApplier.ProcessChangeBatch(resolutionPolicy, sourceChanges, changeDataRetriever, syncCallbacks, syncContext);
      }
      finally {
        activeOperation = null;
      }
    }

    private ChangeBatchBuilder CreateBatchBuilder()
    {
      var metadataManager = session.Services.Demand<MetadataManager>();
      var accessor = session.Services.Demand<DirectEntityAccessor>();
      var tupleFormatters = session.Services.Demand<EntityTupleFormatterRegistry>();
      var changeDetector = new ChangeDetector(ReplicaInfo, configuration, metadataManager, accessor, tupleFormatters);

      return new ChangeBatchBuilder(ReplicaInfo, configuration, metadataManager, changeDetector);
    }

    private ChangeApplier CreateChangerApplier()
    {
      var metadataManager = session.Services.Demand<MetadataManager>();
      var metadataFetcher = session.Services.Demand<MetadataFetcher>();
      var accessor = session.Services.Demand<DirectEntityAccessor>();
      var tickGenerator = session.Services.Demand<SyncTickGenerator>();
      var tupleFormatters = session.Services.Demand<EntityTupleFormatterRegistry>();

      return new ChangeApplier(ReplicaInfo, metadataManager, metadataFetcher, accessor, tickGenerator, tupleFormatters);
    }

    public void Complete()
    {
      if (activeOperation!=null)
        throw new InvalidOperationException(string.Format("Operation '{0}' is still in progress.", activeOperation));

      batchBuilder = null;
      changeApplier = null;

      replicaInfoManager.SaveReplicaInfo(ReplicaInfo);
      session.SaveChanges();
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

      replicaInfoManager = session.Services.Demand<ReplicaInfoManager>();
      ReplicaInfo = replicaInfoManager.LoadReplicaInfo();
    }
  }
}