using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Orm.Model;
using Xtensive.Orm.Providers;
using Xtensive.Orm.Providers.Sql.Mappings;
using Xtensive.Orm.Services;
using Xtensive.Sql;
using Xtensive.Sql.Dml;
using FieldInfo = Xtensive.Orm.Model.FieldInfo;

namespace Xtensive.Orm.BulkOperations
{
  public static class DMLExtensions
  {
    private static readonly MethodInfo translateQueryMethod = typeof (QueryBuilder).GetMethod("TranslateQuery");

    public static int Delete<T>(this IQueryable<T> query) where T : class, IEntity
    {
      var context = new DMLContext<T>(query);
      context.Session.EnsureTransactionIsStarted();
      context.Session.SaveChanges();

      QueryCommand delete = GetDeleteBatchCommand(context);
      int result = delete.ExecuteNonQuery();
      SessionStateAccessor accessor = DirectStateAccessor.Get(context.Session);
      accessor.Invalidate();
      return result;
    }

    [Pure]
    public static IUpdateable<T> Set<T, TResult>(this IQueryable<T> query, Expression<Func<T, TResult>> field,
      Expression<Func<T, TResult>> update)
    {
      return new Updateable<T>(query, field, update);
    }

    [Pure]
    public static IUpdateable<T> Set<T, TResult>(this IUpdateable<T> query, Expression<Func<T, TResult>> field,
      Expression<Func<T, TResult>> update)
    {
      return new Updateable<T>((Updateable<T>) query, field, update);
    }

    [Pure]
    public static IUpdateable<T> Set<T, TResult>(this IQueryable<T> query, Expression<Func<T, TResult>> field,
      TResult value)
    {
      return Set(query, field, a => value);
    }

    [Pure]
    public static IUpdateable<T> Set<T, TResult>(this IUpdateable<T> query, Expression<Func<T, TResult>> field,
      TResult value)
    {
      return Set(query, field, a => value);
    }

    public static int Update<T>(this IQueryable<T> query, Expression<Func<T, T>> evaluator) where T : class, IEntity
    {
      var context = new DMLContext<T>(query);
      List<SetDescriptor> list = null;
      int memberInitCount = 0;
      ParameterExpression parameter = evaluator.Parameters[0];
      evaluator.Visit(
        delegate(MemberInitExpression ex) {
          if (memberInitCount > 0)
            return ex;
          memberInitCount++;
          list = (from MemberAssignment assigment in ex.Bindings
            select
              new SetDescriptor(
                context.TypeInfo.Fields.First(a => a.UnderlyingProperty==assigment.Member),
                parameter,
                assigment.Expression)).ToList();
          return ex;
        });
      return Update(context, list);
    }

    public static int Update<T>(this IUpdateable<T> query) where T : class, IEntity
    {
      var list = new List<SetDescriptor>();
      var q = (Updateable<T>) query;
      var context = new DMLContext<T>(q.Query);
      foreach (var expression in q.Expressions) {
        var lambda = (LambdaExpression) expression.Item1;
        Expression ex = lambda.Body;
        var ex1 = lambda.Body as UnaryExpression;
        if (ex1!=null && ex1.NodeType==ExpressionType.Convert)
          ex = ex1.Operand;
        var member = (PropertyInfo) ((MemberExpression) ex).Member;
        lambda = (LambdaExpression) expression.Item2;
        list.Add(
          new SetDescriptor(
            context.TypeInfo.Fields.First(a => a.UnderlyingProperty==member),
            lambda.Parameters[0],
            lambda.Body));
      }
      return Update(context, list);
    }

    #region Non-public methods

    private static void AddComputedExpression<T>(DMLContext<T> context, DMLAddValueContext addContext)
      where T : class, IEntity
    {
      SqlTableColumn column = SqlDml.TableColumn(addContext.Update.Update, addContext.Field.Column.Name);
      MethodCallExpression selectExpression = Expression.Call(
        typeof (Queryable),
        "OrderBy",
        addContext.Lambda.Type.GetGenericArguments(),
        Expression.Constant(context.Session.Query.All<T>()),
        addContext.Lambda);
      QueryTranslationResult request = GetRequest(context, context.Query.Provider.CreateQuery<T>(selectExpression));
      var sqlSelect = ((SqlSelect) request.Query);
      SqlExpression ex = sqlSelect.OrderBy[0].Expression;
      context.ParameterBindings.AddRange(request.ParameterBindings);

      ex.AcceptVisitor(new ComputedExpressionSqlVisitor(sqlSelect.From, context.TableRef));
      addContext.Update.Values.Add(column, ex);
    }

    private static void AddConstantValue<T>(DMLContext<T> context, DMLAddValueContext addContext)
      where T : class, IEntity
    {
      SqlTableColumn column = SqlDml.TableColumn(addContext.Update.Update, addContext.Field.Column.Name);
      SqlExpression value;
      object constant = Expression.Lambda(addContext.Lambda.Body, null).Compile().DynamicInvoke();
      if (constant==null)
        value = SqlDml.Null;
      else {
        QueryParameterBinding binding = context.QueryBuilder.CreateParameterBinding(constant.GetType(), () => constant);
        context.ParameterBindings.Add(binding);
        value = binding.ParameterReference;
      }
      addContext.Update.Values.Add(column, value);
    }

    private static void AddEntityValue<T>(DMLContext<T> context, DMLAddValueContext addContext)
      where T : class, IEntity
    {
      if (addContext.EntityParamExists)
        throw new NotSupportedException("Expressions with reference to updating entity are not supported");
      var methodCall = addContext.Descriptor.Expression as MethodCallExpression;
      int i;
      if (methodCall!=null) {
        if (methodCall.Method.DeclaringType==typeof (QueryEndpoint) &&
          methodCall.Method.Name.In("Single", "SingleOrDefault")) {
          object[] keys;
          if (methodCall.Arguments[0].Type==typeof (Key) ||
            methodCall.Arguments[0].Type.IsSubclassOf(typeof (Key))) {
            var key = (Key) methodCall.Arguments[0].Invoke();
            keys = new object[key.Value.Count];
            for (i = 0; i < keys.Length; i++)
              keys[i] = key.Value.GetValue(i);
          }
          else
            keys = (object[]) methodCall.Arguments[0].Invoke();
          i = -1;
          foreach (ColumnInfo column in addContext.Field.Columns) {
            i++;
            SqlExpression value;
            if (keys[i]==null)
              value = SqlDml.Null;
            else {
              object v = keys[i];
              QueryParameterBinding binding = context.QueryBuilder.CreateParameterBinding(v.GetType(), () => v);
              context.ParameterBindings.Add(binding);
              value = binding.ParameterReference;
            }
            SqlTableColumn c = SqlDml.TableColumn(addContext.Update.Update, column.Name);
            addContext.Update.Values.Add(c, value);
          }
          return;
        }
        if (methodCall.Method.DeclaringType==typeof (Queryable) &&
          methodCall.Method.Name.In("Single", "SingleOrDefault", "First", "FirstOrDefault")) {
          Expression exp = methodCall.Arguments[0];
          TypeInfo info = GetTypeInfo(context.DomainModel, addContext.Field.ValueType);
          if (methodCall.Arguments.Count==2)
            exp = Expression.Call(
              typeof (Queryable), "Where", new[] {info.UnderlyingType}, exp, methodCall.Arguments[1]);
          exp = Expression.Call(
            typeof (Queryable), "Take", new[] {info.UnderlyingType}, exp, Expression.Constant(1));
          i = -1;
          foreach (FieldInfo field in
            info.Key.Fields) {
            i++;
            ParameterExpression p = Expression.Parameter(info.UnderlyingType);
            LambdaExpression lambda =
              Expression.Lambda(
                typeof (Func<,>).MakeGenericType(info.UnderlyingType, field.ValueType),
                Expression.MakeMemberAccess(p, field.UnderlyingProperty),
                p);
            IQueryable q =
              context.Query.Provider.CreateQuery(
                Expression.Call(
                  typeof (Queryable),
                  "Select",
                  new[] {info.UnderlyingType, field.ValueType},
                  exp,
                  lambda));
            QueryTranslationResult request = GetRequest(field.ValueType, context, q);
            context.ParameterBindings.AddRange(request.ParameterBindings);
            SqlTableColumn c = SqlDml.TableColumn(
              addContext.Update.Update, addContext.Field.Columns[i].Name);
            addContext.Update.Values.Add(c, SqlDml.SubQuery((ISqlQueryExpression) request.Query));
          }
          return;
        }
      }
      i = -1;
      var entity = (IEntity) Expression.Lambda(addContext.Lambda.Body, null).Compile().DynamicInvoke();
      foreach (ColumnInfo column in addContext.Field.Columns) {
        i++;
        SqlExpression value;
        if (entity==null)
          value = SqlDml.Null;
        else {
          object v = entity.Key.Value.GetValue(i);
          QueryParameterBinding binding = context.QueryBuilder.CreateParameterBinding(v.GetType(), () => v);
          context.ParameterBindings.Add(binding);
          value = binding.ParameterReference;
        }
        SqlTableColumn c = SqlDml.TableColumn(addContext.Update.Update, column.Name);
        addContext.Update.Values.Add(c, value);
      }
    }

    private static void AddStructureValue<T>(DMLContext<T> context, DMLAddValueContext addContext)
    {
      foreach (FieldInfo field in addContext.Field.Fields) {
      }
    }

    private static void AddValues<T>(DMLContext<T> context, SqlUpdate update) where T : class, IEntity
    {
      foreach (SetDescriptor descriptor in context.SetDescriptors) {
        var addContext = new DMLAddValueContext {
          Descriptor = descriptor,
          Lambda =
            Expression.Lambda(
              typeof (Func<,>).MakeGenericType(typeof (T), descriptor.Expression.Type),
              descriptor.Expression,
              descriptor.Parameter),
          Update = update
        };
        descriptor.Expression.Visit(
          delegate(ParameterExpression p) {
            // ReSharper disable AccessToModifiedClosure
            if (p==descriptor.Parameter)
              // ReSharper restore AccessToModifiedClosure
              addContext.EntityParamExists = true;
            return p;
          });
        addContext.SubqueryExists = descriptor.Expression.IsContainsQuery();
        addContext.Field = descriptor.Field;
        if (addContext.Field.IsEntitySet)
          throw new NotSupportedException("EntitySets are not supported");
        if (addContext.Field.IsStructure) {
          AddStructureValue(context, addContext);
          continue;
        }
        if (addContext.Field.IsEntity) {
          AddEntityValue(context, addContext);
          continue;
        }
        if (addContext.EntityParamExists || addContext.SubqueryExists)
          AddComputedExpression(context, addContext);
        else
          AddConstantValue(context, addContext);
      }
    }

    private static QueryCommand GetDeleteBatchCommand<T>(DMLContext<T> context) where T : class, IEntity
    {
      QueryTranslationResult request = GetRequest(context, context.Query);
      context.ParameterBindings = request.ParameterBindings.ToList();
      if (context.PrimaryIndexes.Length > 1)
        throw new NotImplementedException("Inheritance is not implemented");
      SqlDelete delete = SqlDml.Delete(SqlDml.TableRef(context.PrimaryIndexes.First().Table));
      Join(context, delete, (SqlSelect) request.Query);

      return ToCommand(context, delete);
    }

    private static QueryTranslationResult GetRequest<T>(DMLContext<T> context, IQueryable<T> query) where T : class, IEntity
    {
      return context.QueryBuilder.TranslateQuery(query);
    }

    private static QueryTranslationResult GetRequest<T>(Type type, DMLContext<T> context, IQueryable query) where T : class, IEntity
    {
      throw new ApplicationException();
      return (QueryTranslationResult) translateQueryMethod.MakeGenericMethod(type).Invoke(context.QueryBuilder, new[] {query});
    }

    private static TypeInfo GetTypeInfo(DomainModel model, Type entityType)
    {
      return model.Hierarchies.SelectMany(a => a.Types).Single(a => a.UnderlyingType==entityType);
    }

    private static QueryCommand GetUpdateBatchCommand<T>(DMLContext<T> context) where T : class, IEntity
    {
      QueryTranslationResult request = GetRequest(context, context.Query);
      context.ParameterBindings = request.ParameterBindings.ToList();
      if (context.PrimaryIndexes.Length > 1)
        throw new NotImplementedException("Inheritance is not implemented");
      SqlUpdate update = SqlDml.Update(SqlDml.TableRef(context.PrimaryIndexes.First().Table));
      Join(context, update, (SqlSelect) request.Query);

      AddValues(context, update);

      return ToCommand(context, update);
    }

    private static void Join<T>(DMLContext<T> context, SqlQueryStatement statement, SqlSelect select)
      where T : class, IEntity
    {
      var update = statement as SqlUpdate;
      var delete = statement as SqlDelete;
      if (update==null && delete==null)
        throw new ArgumentException();
      var sqlTableRef = @select.From as SqlTableRef;
      if (sqlTableRef!=null) {
        if (update!=null) {
          update.Where = @select.Where;
          update.Update = sqlTableRef;
          update.From = sqlTableRef;
        }
        else {
          delete.Where = @select.Where;
          delete.Delete = sqlTableRef;
          delete.From = sqlTableRef;
        }
        context.TableRef = sqlTableRef;
        return;
      }

      bool b = (update!=null && context.DomainHandler.ProviderInfo.Supports(ProviderFeatures.UpdateFrom));
      if (!b)
        b = (delete!=null && context.DomainHandler.ProviderInfo.Supports(ProviderFeatures.DeleteFrom));
      if (b)
        JoinViaJoin(context, update, delete, @select);
      else
        JoinViaIn(context, update, delete, select);
    }

    private static void JoinViaIn<T>(DMLContext<T> context, SqlUpdate update, SqlDelete delete, SqlSelect @select)
    {
      SqlTableRef table;
      SqlExpression where;
      if (update!=null) {
        table = update.Update;
        where = update.Where;
      }
      else {
        table = delete.Delete;
        where = delete.Where;
      }
      context.TableRef = table;
      PrimaryIndexMapping indexMapping = context.PrimaryIndexes[0];
      var columns = new List<ColumnInfo>();
      foreach (ColumnInfo columnInfo in indexMapping.PrimaryIndex.KeyColumns.Keys) {
        SqlSelect s = select.ShallowClone();
        foreach (ColumnInfo column in columns) {
          SqlBinary ex = SqlDml.Equals(
            SqlDml.TableColumn(s.From, column.Name), SqlDml.TableColumn(table, column.Name));
          s.Where = s.Where.IsNullReference() ? ex : SqlDml.And(s.Where, ex);
        }
        s.Columns.Clear();
        s.Columns.Add(SqlDml.TableColumn(s.From, columnInfo.Name));
        SqlBinary @in = SqlDml.In(SqlDml.TableColumn(table, columnInfo.Name), s);
        @where = @where.IsNullReference() ? @in : SqlDml.And(@where, @in);
        columns.Add(columnInfo);
      }
      if (update!=null)
        update.Where = where;
      else
        delete.Where = where;
    }

    private static void JoinViaJoin<T>(DMLContext<T> context, SqlUpdate update, SqlDelete delete, SqlSelect @select)
      where T : class, IEntity
    {
      PrimaryIndexMapping indexMapping = context.PrimaryIndexes[0];
      SqlTableRef left = SqlDml.TableRef(indexMapping.Table);
      SqlQueryRef right = SqlDml.QueryRef(@select);
      SqlExpression joinExpression = null;
      for (int i = 0; i < indexMapping.PrimaryIndex.KeyColumns.Count; i++) {
        SqlBinary binary = (left.Columns[i]==right.Columns[i]);
        if (joinExpression.IsNullReference())
          joinExpression = binary;
        else
          joinExpression &= binary;
      }
      context.TableRef = left;
      SqlJoinedTable joinedTable = left.InnerJoin(right, joinExpression);
      if (update!=null)
        update.From = joinedTable;
      else
        delete.From = joinedTable;
      return;
    }

    private static void PreprocessStructures<T>(DMLContext<T> context)
    {
      bool changed = true;
      while (changed) {
        changed = false;
        foreach (SetDescriptor setDescriptor in context.SetDescriptors.Where(a => a.Field.IsStructure).ToArray()
          ) {
          var memberInit = setDescriptor.Expression as MemberInitExpression;
          if (memberInit!=null) {
            changed = true;
            context.SetDescriptors.Remove(setDescriptor);
            foreach (MemberAssignment binding in memberInit.Bindings) {
              FieldInfo f =
                setDescriptor.Field.Fields.First(a => a.UnderlyingProperty==binding.Member);
              context.SetDescriptors.Add(
                new SetDescriptor(f, setDescriptor.Parameter, binding.Expression));
            }
          }
          else
            foreach (FieldInfo f in setDescriptor.Field.Fields.Where(a => !a.IsStructure)) {
              changed = true;
              string name = f.Name;
              if (setDescriptor.Field.IsStructure)
                name = name.Remove(0, setDescriptor.Field.Name.Length + 1);
              context.SetDescriptors.Remove(setDescriptor);
              Expression ex = setDescriptor.Expression;
              var call = ex as MethodCallExpression;
              if (call!=null && call.Method.DeclaringType==typeof (Queryable) &&
                call.Method.Name.In("First", "FirstOrDefault", "Single", "SingleOrDefault"))
                throw new NotSupportedException("Subqueries with structures are not supported");
                /*ex = call.Arguments[0];
            ParameterExpression parameter = Expression.Parameter(setDescriptor.Expression.Type, "parameter");
            var list = new List<Model.FieldInfo> {f};
            while (list.Last().Parent != setDescriptor.Field)
                list.Add(f.Parent);
            list.Reverse();
            Expression member = parameter;
            foreach (Model.FieldInfo f2 in list)
                member = Expression.MakeMemberAccess(member, f2.UnderlyingProperty);
            LambdaExpression lambda =
                Expression.Lambda(
                    typeof (Func<,>).MakeGenericType(parameter.Type, f.ValueType), member, parameter);
            ex = Expression.Call(
                typeof (Queryable), "Select", new[] {parameter.Type, f.ValueType}, ex, lambda);
            ex = Expression.Call(typeof (Queryable), call.Method.Name, new[] {f.ValueType}, ex);*/
              else {
                //ex = Expression.Convert(ex, typeof(Structure));
                var list = new List<FieldInfo> {f};
                while (list.Last().Parent!=setDescriptor.Field)
                  list.Add(f.Parent);
                list.Reverse();
                Expression member = ex;
                foreach (FieldInfo f2 in list)
                  member = Expression.MakeMemberAccess(member, f2.UnderlyingProperty);
                ex = member;
              }
              context.SetDescriptors.Add(new SetDescriptor(f, setDescriptor.Parameter, ex));
            }
        }
      }
    }

    private static QueryCommand ToCommand<T>(DMLContext<T> context, SqlStatement statement) where T : class, IEntity
    {
      return context.QueryBuilder.CreateCommand(context.QueryBuilder.CreateRequest(context.QueryBuilder.CompileQuery((ISqlCompileUnit) statement), context.ParameterBindings));
    }

    private static int Update<T>(DMLContext<T> context, IEnumerable<SetDescriptor> expressions)
      where T : class, IEntity
    {
      context.Session.EnsureTransactionIsStarted();
      context.SetDescriptors = expressions.ToList();
      context.Session.SaveChanges();
      PreprocessStructures(context);
      QueryCommand update = GetUpdateBatchCommand(context);
      int result = update.ExecuteNonQuery();
      SessionStateAccessor accessor = DirectStateAccessor.Get(context.Session);
      accessor.Invalidate();
      return result;
    }

    #endregion
  }
}