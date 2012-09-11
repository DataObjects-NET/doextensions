using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.IoC;
using Xtensive.Orm.Tracking;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (MetadataUpdater), Singleton = false)]
  internal sealed class MetadataUpdater : ISessionService
  {
    private readonly MetadataManager metadataManager;
    private readonly ReplicaManager replicaManager;

    private ReplicaState replicaState;

    public void ProcessEntityChanges(ICollection<ITrackingItem> itemsToProcess)
    {
      if (replicaState==null)
        replicaState = replicaManager.LoadReplicaState();

      var lookup = metadataManager
        .GetMetadata(itemsToProcess.Select(i => i.Key))
        .ToDictionary(i => i.SyncTargetKey);

      foreach (var item in itemsToProcess) {
        if (item.State==TrackingItemState.Created)
          metadataManager.CreateMetadata(item.Key, replicaState);
        else {
          SyncInfo syncInfo;
          if (lookup.TryGetValue(item.Key, out syncInfo))
            metadataManager.UpdateMetadata(syncInfo, item.State==TrackingItemState.Deleted);
          else
            metadataManager.CreateMetadata(item.Key, replicaState);
        }
      }
    }

    [ServiceConstructor]
    public MetadataUpdater(MetadataManager metadataManager, ReplicaManager replicaManager)
    {
      if (metadataManager==null)
        throw new ArgumentNullException("metadataManager");
      if (replicaManager==null)
        throw new ArgumentNullException("replicaManager");

      this.metadataManager = metadataManager;
      this.replicaManager = replicaManager;
    }
  }
}