using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  internal sealed class MetadataQuery
  {
    public SyncId MinId { get; private set; }

    public SyncId MaxId { get; private set; }

    public IList<MetadataQueryFilter> Filters { get; private set; }

    public override string ToString()
    {
      var result = new StringBuilder();
      result.AppendFormat("[{0}, {1})", MinId, MaxId);
      if (Filters!=null)
        result.AppendFormat(" where {0}", string.Join(" or ", Filters.Select(f => string.Format("({0})", f))));
      return result.ToString();
    }

    public MetadataQuery(SyncId minId, SyncId maxId, IEnumerable<MetadataQueryFilter> filters = null)
    {
      if (minId==null)
        throw new ArgumentNullException("minId");
      if (maxId==null)
        throw new ArgumentNullException("maxId");

      MinId = minId;
      MaxId = maxId;

      if (filters==null)
        return;

      Filters = filters.ToList().AsReadOnly();
      if (Filters.Count==0)
        Filters = null;
    }
  }
}