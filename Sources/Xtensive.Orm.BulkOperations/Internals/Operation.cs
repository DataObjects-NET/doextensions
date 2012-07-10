using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using Xtensive.Core;
using Xtensive.Orm.Linq;
using Xtensive.Orm.Model;
using Xtensive.Orm.Providers;
using Xtensive.Orm.Services;
using Xtensive.Sql.Model;
using QueryParameterBinding = Xtensive.Orm.Services.QueryParameterBinding;

namespace Xtensive.Orm.BulkOperations
{
  internal abstract class Operation<T>
    where T : class, IEntity
  {
    private static readonly MethodInfo TranslateQueryMethod = typeof (QueryBuilder).GetMethod("TranslateQuery");
    protected readonly QueryProvider QueryProvider;
    protected List<QueryParameterBinding> Bindings;
    protected DomainHandler DomainHandler;
    protected PrimaryIndexMapping[] PrimaryIndexes;
    protected QueryBuilder QueryBuilder;
    protected Session Session;
    protected TypeInfo TypeInfo;

    public int Execute()
    {
      EnsureTransactionIsStarted();
      QueryProvider.Session.SaveChanges();
      int value = ExecuteInternal();
      SessionStateAccessor accessor = DirectStateAccessor.Get(QueryProvider.Session);
      accessor.Invalidate();
      return value;
    }

    #region Non-public methods

    protected void EnsureTransactionIsStarted()
    {
      var accessor = QueryProvider.Session.Services.Demand<DirectSqlAccessor>();
#pragma warning disable 168
      DbTransaction notUsed = accessor.Transaction;
#pragma warning restore 168
    }

    protected abstract int ExecuteInternal();

    protected QueryTranslationResult GetRequest(IQueryable<T> query)
    {
      return QueryBuilder.TranslateQuery(query);
    }

    protected QueryTranslationResult GetRequest(Type type, IQueryable query)
    {
      return
        (QueryTranslationResult) TranslateQueryMethod.MakeGenericMethod(type).Invoke(QueryBuilder, new object[] {query});
    }

    protected TypeInfo GetTypeInfo(Type entityType)
    {
      return Session.Domain.Model.Hierarchies.SelectMany(a => a.Types).Single(a => a.UnderlyingType==entityType);
    }

    #endregion

    protected Operation(QueryProvider queryProvider)
    {
      QueryProvider = queryProvider;
      Type entityType = typeof (T);
      Session = queryProvider.Session;
      DomainHandler = (DomainHandler) Session.Domain.Services.Get<Providers.DomainHandler>();
      TypeInfo =
        queryProvider.Session.Domain.Model.Hierarchies.SelectMany(a => a.Types).Single(
          a => a.UnderlyingType==entityType);
      PrimaryIndexes = TypeInfo.AffectedIndexes
        .Where(i => i.IsPrimary)
        .Select(i => new PrimaryIndexMapping(i, DomainHandler.Mapping[i.ReflectedType]))
        .ToArray();
      QueryBuilder = Session.Services.Get<QueryBuilder>();
    }
  }
}