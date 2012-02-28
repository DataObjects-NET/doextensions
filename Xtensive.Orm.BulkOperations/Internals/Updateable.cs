using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Xtensive.Orm.BulkOperations
{
  internal class Updateable<T> : IUpdateable<T>
  {
    internal readonly List<Tuple<Expression, Expression>> Expressions;
    internal readonly IQueryable<T> Query;

    public Updateable(Updateable<T> updateable, Expression field, Expression update)
    {
      Query = updateable.Query;
      Expressions = new List<Tuple<Expression, Expression>>(updateable.Expressions.Count + 1);
      Expressions.AddRange(updateable.Expressions);
      Expressions.Add(Tuple.Create(field, update));
    }

    public Updateable(IQueryable<T> query, Expression field, Expression update)
    {
      Query = query;
      Expressions = new List<Tuple<Expression, Expression>>(1) {Tuple.Create(field, update)};
    }
  }
}