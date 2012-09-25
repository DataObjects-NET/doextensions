using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Synchronization;
using Xtensive.Orm.Sync.Model;

namespace Xtensive.Orm.Sync
{
  internal sealed class MetadataQueryGroup : IEnumerable<MetadataQuery>
  {
    private readonly List<MetadataQuery> queries = new List<MetadataQuery>();
    private readonly MetadataStore store;
    private readonly Expression userFilter;

    public int Count { get { return queries.Count; } }

    public SyncId MinResultId { get { return store.MinItemId; } }
    public SyncId MaxResultId { get { return store.MaxItemId; } }

    public void Add(MetadataQuery query)
    {
      queries.Add(query);
    }

    public IEnumerable<SyncInfo> ExecuteAll()
    {
      return queries.SelectMany(query => store.GetOrderedMetadata(query, userFilter));
    }

    #region IEnumerable

    public IEnumerator<MetadataQuery> GetEnumerator()
    {
      return queries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    #endregion

    public MetadataQueryGroup(MetadataStore store, Expression userFilter)
    {
      this.store = store;

      this.userFilter = userFilter;
    }
  }
}