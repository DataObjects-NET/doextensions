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

    public IList<SyncVersion> LastKnownVersions { get; private set; }

    public IEnumerable<SyncInfo> Execute()
    {
      return store.GetOrderedMetadata(this);
    }

    public override string ToString()
    {
      if (string.IsNullOrEmpty(MinId))
        return string.Format("{0} [all]", store.EntityType);
      return string.Format("{0} [{1}, {2})", store.EntityType.Name, MinId, MaxId);
    }

    public MetadataQueryTask(MetadataStore store, Expression filter)
    {
      if (store==null)
        throw new ArgumentNullException("store");

      this.store = store;
      UserFilter = filter;
    }

    public MetadataQueryTask(
      MetadataStore store, string minId, string maxId,
      IEnumerable<SyncVersion> lastKnownVersions, Expression userFilter)
    {
      if (store==null)
        throw new ArgumentNullException("store");
      if (minId==null)
        throw new ArgumentNullException("minId");
      if (maxId==null)
        throw new ArgumentNullException("maxId");
      if (lastKnownVersions==null)
        throw new ArgumentNullException("lastKnownVersions");

      this.store = store;
      MinId = minId;
      MaxId = maxId;
      LastKnownVersions = lastKnownVersions.ToList().AsReadOnly();
      UserFilter = userFilter;
    }
  }
}