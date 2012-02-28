using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Orm.Linq;
using Xtensive.Orm.Model;
using Xtensive.Orm.Providers.Sql;
using Xtensive.Orm.Providers.Sql.Mappings;
using Xtensive.Orm.Services;
using Xtensive.Sql.Dml;
using QueryParameterBinding = Xtensive.Orm.Services.QueryParameterBinding;

namespace Xtensive.Orm.BulkOperations
{
  internal class DMLContext<T>
  {
    public DMLContext(IQueryable<T> query)
    {
      Query = query;
      var provider = (QueryProvider)query.Provider;
      Session = provider.Session;
      DomainModel = Session.Domain.Model;
      Type entityType = typeof(T);
      TypeInfo = DomainModel.Hierarchies.SelectMany(a => a.Types).Single(a => a.UnderlyingType == entityType);
      DomainHandler = (DomainHandler)Session.Domain.Services.Get<Providers.DomainHandler>();
      SessionHandler = (SessionHandler)Session.Services.Get<Providers.SessionHandler>();
      Driver = DomainHandler.Driver;
      PrimaryIndexes =
          TypeInfo.AffectedIndexes.Where(a => a.IsPrimary).Select(a => DomainHandler.Mapping[a]).ToArray();
      QueryBuilder = Session.Services.Get<QueryBuilder>();
    }

    public QueryBuilder QueryBuilder { get; set; }

    public PrimaryIndexMapping[] PrimaryIndexes { get; private set; }

    public StorageDriver Driver { get; private set; }
    public List<SetDescriptor> SetDescriptors { get; set; }
    public Session Session { get; private set; }
    public DomainModel DomainModel { get; private set; }
    public TypeInfo TypeInfo { get; private set; }
    public IQueryable<T> Query { get; private set; }
    public DomainHandler DomainHandler { get; private set; }
    public SessionHandler SessionHandler { get; private set; }
    public List<QueryParameterBinding> ParameterBindings { get; set; }


    public SqlTableRef TableRef { get; set; }
  }
}