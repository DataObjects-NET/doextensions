using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xtensive.Orm.Linq;
using Xtensive.Orm.Model;
using Xtensive.Orm.Services;
using Xtensive.Sql;
using Xtensive.Sql.Dml;

namespace Xtensive.Orm.BulkOperations
{
  internal class InsertOperation<T> : Operation<T>
    where T : Entity
  {
    private readonly SetOperation<T> setOperation;

    #region Non-public methods

    protected override int ExecuteInternal()
    {
      Bindings = new List<QueryParameterBinding>();
      if (PrimaryIndexes.Length > 1)
        throw new NotImplementedException("Inheritance is not implemented");
      SqlInsert insert = SqlDml.Insert(SqlDml.TableRef(PrimaryIndexes[0].Table));
      setOperation.Statement = SetStatement.Create(insert);
      setOperation.AddValues();
      QueryCommand command = ToCommand(insert);
      return command.ExecuteNonQuery();
    }

    #endregion

    public InsertOperation(QueryProvider queryProvider, Expression<Func<T>> evaluator)
      : base(queryProvider)
    {
      int memberInitCount = 0;
      ParameterExpression parameter = Expression.Parameter(typeof (T));
      List<SetDescriptor> descriptors = null;
      evaluator.Visit(
        delegate(MemberInitExpression ex) {
          if (memberInitCount > 0)
            return ex;
          memberInitCount++;
          descriptors = (from MemberAssignment assigment in ex.Bindings
                         let propertyInfo = (assigment.Member==TypeInfo.UnderlyingType) 
                           ? assigment.Member 
                           : TypeInfo.UnderlyingType.GetProperty(assigment.Member.Name)
                         select new SetDescriptor(TypeInfo.Fields.First(a => a.UnderlyingProperty==propertyInfo), parameter, assigment.Expression)).ToList();
          return ex;
        });
      AddKey(descriptors);
      setOperation = new SetOperation<T>(this, descriptors);
    }

    private void AddKey(List<SetDescriptor> descriptors)
    {
      var count = descriptors.Count(a => a.Field.IsPrimaryKey);
      int i;
      if (count==0) {
        var key = Key.Generate<T>(Session);
        i = 0;
        foreach (var fieldInfo in TypeInfo.Key.Fields) {
          descriptors.Add(new SetDescriptor(fieldInfo, Expression.Parameter(typeof(T)), Expression.Constant(key.Value.GetValue(i))));
          i++;
        }
        Key = key;
        return;
      }
      if(count<TypeInfo.Key.Fields.Count()) {
        throw new InvalidOperationException("You must set 0 or all key fields");
      }
      i = 0;
      var keys = new object[TypeInfo.Key.Fields.Count];
      foreach(var field in TypeInfo.Key.Fields) {
        var descriptor = descriptors.First(a => a.Field.Equals(field));
        keys[i] = descriptor.Expression.Invoke();
        i++;
      }
      Key = Key.Create<T>(Session.Domain, keys);
    }

    public Key Key { get; private set; }
  }
}