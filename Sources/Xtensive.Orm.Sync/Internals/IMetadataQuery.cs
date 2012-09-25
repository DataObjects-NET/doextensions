using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  internal interface IMetadataQuery
  {
    string MinId { get; }

    string MaxId { get; }

    Expression UserFilter { get; }

    SyncVersion LastKnownVersion { get; }

    IList<uint> ReplicasToExclude { get; }
  }
}