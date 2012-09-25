using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Synchronization;
using Xtensive.Orm.Sync.Model;

namespace Xtensive.Orm.Sync
{
  internal sealed class MetadataQueryTask : IMetadataQuery
  {
    private readonly MetadataStore store;

    public string MinId { get; private set; }

    public string MaxId { get; private set; }

    public Expression UserFilter { get; private set; }

    public SyncVersion LastKnownVersion { get; private set; }

    public IList<uint> ReplicasToExclude { get; private set; }

    public IEnumerable<SyncInfo> Execute()
    {
      return store.GetOrderedMetadata(this);
    }

    public MetadataQueryTask(
      MetadataStore store, Expression userFilter,
      string minId = null, string maxId = null,
      SyncVersion lastKnownVersion = null, IEnumerable<uint> replicasToExclude = null)
    {
      if (store==null)
        throw new ArgumentNullException("store");

      this.store = store;

      UserFilter = userFilter;
      MinId = minId;
      MaxId = maxId;
      LastKnownVersion = lastKnownVersion;

      if (replicasToExclude!=null)
        ReplicasToExclude = replicasToExclude.ToList().AsReadOnly();
    }
  }
}