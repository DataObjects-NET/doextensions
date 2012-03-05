using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Orm.Linq;
using Xtensive.Orm.Model;
using Xtensive.Orm.Providers;
using Xtensive.Orm.Services;
using Xtensive.Sql;
using Xtensive.Sql.Dml;
using FieldInfo = Xtensive.Orm.Model.FieldInfo;

namespace Xtensive.Orm.BulkOperations
{
  internal class BulkUpdateOperation<T> : QueryOperation<T>
    where T : class, IEntity
  {
    private readonly IQueryable<T> query;
    private List<SetDescriptor> descriptors;

    #region Non-public methods

    protected override int ExecuteInternal()
    {
      PreprocessStructures();
      QueryTranslationResult request = GetRequest(query);
      Bindings = request.ParameterBindings.ToList();
      if (PrimaryIndexes.Length > 1)
        throw new NotImplementedException("Inheritance is not implemented");
      SqlUpdate update = SqlDml.Update(SqlDml.TableRef(PrimaryIndexes[0].Table));
      Join(update, (SqlSelect) request.Query);
      AddValues(update);
      QueryCommand command = ToCommand(update);
      return command.ExecuteNonQuery();
    }

    protected override SqlTableRef GetStatementTable(SqlStatement statement)
    {
      var update = (SqlUpdate) statement;
      return update.Update;
    }

    protected override SqlExpression GetStatementWhere(SqlStatement statement)
    {
      var update = (SqlUpdate) statement;
      return update.Where;
    }

    protected override void SetStatementFrom(SqlStatement statement, SqlTable from)
    {
      var update = (SqlUpdate) statement;
      update.From = from;
    }

    protected override void SetStatementTable(SqlStatement statement, SqlTableRef table)
    {
      var update = (SqlUpdate) statement;
      update.Update = table;
    }

    protected override void SetStatementWhere(SqlStatement statement, SqlExpression @where)
    {
      var update = (SqlUpdate) statement;
      update.Where = where;
    }

    protected override bool SupportsJoin()
    {
      return DomainHandler.ProviderInfo.Supports(ProviderFeatures.UpdateFrom);
    }

    private void AddComputedExpression(AddValueContext addContext)
    {
      SqlTableColumn column = SqlDml.TableColumn(addContext.Update.Update, addContext.Field.Column.Name);
      MethodCallExpression selectExpression = Expression.Call(
        typeof (Queryable),
        "OrderBy",
        addContext.Lambda.Type.GetGenericArguments(),
        Expression.Constant(Session.Query.All<T>()),
        addContext.Lambda);
      QueryTranslationResult request = GetRequest(query.Provider.CreateQuery<T>(selectExpression));
      var sqlSelect = ((SqlSelect) request.Query);
      SqlExpression ex = sqlSelect.OrderBy[0].Expression;
      Bindings.AddRange(request.ParameterBindings);

      ex.AcceptVisitor(new ComputedExpressionSqlVisitor(sqlSelect.From, TableRef));
      addContext.Update.Values.Add(column, ex);
    }

    private void AddConstantValue(AddValueContext addContext)
    {
      SqlTableColumn column = SqlDml.TableColumn(addContext.Update.Update, addContext.Field.Column.Name);
      SqlExpression value;
      object constant = Expression.Lambda(addContext.Lambda.Body, null).Compile().DynamicInvoke();
      if (constant==null)
        value = SqlDml.Null;
      else {
        QueryParameterBinding binding = QueryBuilder.CreateParameterBinding(constant.GetType(), () => constant);
        Bindings.Add(binding);
        value = binding.ParameterReference;
      }
      addContext.Update.Values.Add(column, value);
    }

    private void AddEntityValue(AddValueContext addContext)
    {
      if (addContext.EntityParamExists)
        throw new NotSupportedException("Expressions with reference to updating entity are not supported");
      var methodCall = addContext.Descriptor.Expression as MethodCallExpression;
      int i;
      if (methodCall!=null) {
        if (methodCall.Method.DeclaringType==typeof (QueryEndpoint) &&
          methodCall.Method.Name.In("Single", "SingleOrDefault")) {
          object[] keys;
          if (methodCall.Arguments[0].Type==typeof (Key) || methodCall.Arguments[0].Type.IsSubclassOf(typeof (Key))) {
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
              QueryParameterBinding binding = QueryBuilder.CreateParameterBinding(v.GetType(), () => v);
              Bindings.Add(binding);
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
          TypeInfo info = GetTypeInfo(addContext.Field.ValueType);
          if (methodCall.Arguments.Count==2)
            exp = Expression.Call(
              typeof (Queryable), "Where", new[] {info.UnderlyingType}, exp, methodCall.Arguments[1]);
          exp = Expression.Call(typeof (Queryable), "Take", new[] {info.UnderlyingType}, exp, Expression.Constant(1));
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
              query.Provider.CreateQuery(
                Expression.Call(typeof (Queryable), "Select", new[] {info.UnderlyingType, field.ValueType}, exp, lambda));
            QueryTranslationResult request = GetRequest(field.ValueType, q);
            Bindings.AddRange(request.ParameterBindings);
            SqlTableColumn c = SqlDml.TableColumn(addContext.Update.Update, addContext.Field.Columns[i].Name);
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
          QueryParameterBinding binding = QueryBuilder.CreateParameterBinding(v.GetType(), () => v);
          Bindings.Add(binding);
          value = binding.ParameterReference;
        }
        SqlTableColumn c = SqlDml.TableColumn(addContext.Update.Update, column.Name);
        addContext.Update.Values.Add(c, value);
      }
    }


    private void AddValues(SqlUpdate update)
    {
      foreach (SetDescriptor descriptor in descriptors) {
        var addContext = new AddValueContext {
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
        if (addContext.Field.IsEntity) {
          AddEntityValue(addContext);
          continue;
        }
        if (addContext.EntityParamExists || addContext.SubqueryExists)
          AddComputedExpression(addContext);
        else
          AddConstantValue(addContext);
      }
    }

    private void PreprocessStructures()
    {
      bool changed = true;
      while (changed) {
        changed = false;
        foreach (SetDescriptor setDescriptor in descriptors.Where(a => a.Field.IsStructure).ToArray()) {
          var memberInit = setDescriptor.Expression as MemberInitExpression;
          if (memberInit!=null) {
            changed = true;
            descriptors.Remove(setDescriptor);
            foreach (MemberAssignment binding in memberInit.Bindings) {
              FieldInfo f = setDescriptor.Field.Fields.First(a => a.UnderlyingProperty==binding.Member);
              descriptors.Add(new SetDescriptor(f, setDescriptor.Parameter, binding.Expression));
            }
          }
          else
            foreach (FieldInfo f in setDescriptor.Field.Fields.Where(a => !a.IsStructure)) {
              changed = true;
              string name = f.Name;
              if (setDescriptor.Field.IsStructure)
                name = name.Remove(0, setDescriptor.Field.Name.Length + 1);
              descriptors.Remove(setDescriptor);
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
              descriptors.Add(new SetDescriptor(f, setDescriptor.Parameter, ex));
            }
        }
      }
    }

    #endregion

    public BulkUpdateOperation(IQueryable<T> query, List<SetDescriptor> descriptors)
      : base((QueryProvider) query.Provider)
    {
      this.query = query;
      this.descriptors = descriptors;
    }

    public BulkUpdateOperation(IQueryable<T> query, Expression<Func<T, T>> evaluator)
      : base((QueryProvider) query.Provider)
    {
      this.query = query;
      int memberInitCount = 0;
      ParameterExpression parameter = evaluator.Parameters[0];
      evaluator.Visit(
        delegate(MemberInitExpression ex) {
          if (memberInitCount > 0)
            return ex;
          memberInitCount++;
          descriptors = (from MemberAssignment assigment in ex.Bindings
            select
              new SetDescriptor(
                TypeInfo.Fields.First(a => a.UnderlyingProperty==assigment.Member), parameter, assigment.Expression)).
            ToList();
          return ex;
        });
    }

    public BulkUpdateOperation(IUpdatable<T> query)
      : base((QueryProvider) ((Updatable<T>) query).Query.Provider)
    {
      descriptors = new List<SetDescriptor>();
      var q = (Updatable<T>) query;
      this.query = q.Query;
      foreach (var expression in q.Expressions) {
        var lambda = (LambdaExpression) expression.Item1;
        Expression ex = lambda.Body;
        var ex1 = lambda.Body as UnaryExpression;
        if (ex1!=null && ex1.NodeType==ExpressionType.Convert)
          ex = ex1.Operand;
        var member = (PropertyInfo) ((MemberExpression) ex).Member;
        lambda = (LambdaExpression) expression.Item2;
        descriptors.Add(
          new SetDescriptor(TypeInfo.Fields.First(a => a.UnderlyingProperty==member), lambda.Parameters[0], lambda.Body));
      }
    }
  }
}