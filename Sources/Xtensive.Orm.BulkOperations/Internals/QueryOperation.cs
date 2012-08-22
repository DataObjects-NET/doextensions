using System.Collections.Generic;
using Xtensive.Orm.Linq;
using Xtensive.Orm.Model;
using Xtensive.Orm.Services;
using Xtensive.Sql;
using Xtensive.Sql.Dml;

namespace Xtensive.Orm.BulkOperations
{
  internal abstract class QueryOperation<T> : Operation<T>
    where T : class, IEntity
  {
    #region Non-public methods

    protected abstract SqlTableRef GetStatementTable(SqlStatement statement);
    protected abstract SqlExpression GetStatementWhere(SqlStatement statement);

    protected void Join(SqlQueryStatement statement, SqlSelect select)
    {
      var sqlTableRef = @select.From as SqlTableRef;
      if (sqlTableRef!=null) {
        SetStatementTable(statement, sqlTableRef);
        SetStatementWhere(statement, select.Where);
        JoinedTableRef = sqlTableRef;
        return;
      }

      if (SupportsJoin())
        JoinViaJoin(statement, select);
      else
        JoinViaIn(statement, select);
    }

    protected abstract void SetStatementFrom(SqlStatement statement, SqlTable from);
    protected abstract void SetStatementTable(SqlStatement statement, SqlTableRef table);
    protected abstract void SetStatementWhere(SqlStatement statement, SqlExpression where);
    protected abstract bool SupportsJoin();


    private void JoinViaIn(SqlStatement statement, SqlSelect @select)
    {
      SqlTableRef table = GetStatementTable(statement);
      SqlExpression where = GetStatementWhere(statement);
      JoinedTableRef = table;
      PrimaryIndexMapping indexMapping = PrimaryIndexes[0];
      var columns = new List<ColumnInfo>();
      foreach (ColumnInfo columnInfo in indexMapping.PrimaryIndex.KeyColumns.Keys) {
        SqlSelect s = select.ShallowClone();
        foreach (ColumnInfo column in columns) {
          SqlBinary ex = SqlDml.Equals(SqlDml.TableColumn(s.From, column.Name), SqlDml.TableColumn(table, column.Name));
          s.Where = s.Where.IsNullReference() ? ex : SqlDml.And(s.Where, ex);
        }
        s.Columns.Clear();
        s.Columns.Add(SqlDml.TableColumn(s.From, columnInfo.Name));
        SqlBinary @in = SqlDml.In(SqlDml.TableColumn(table, columnInfo.Name), s);
        @where = @where.IsNullReference() ? @in : SqlDml.And(@where, @in);
        columns.Add(columnInfo);
      }
      SetStatementWhere(statement, where);
    }

    private void JoinViaJoin(SqlStatement statement, SqlSelect @select)
    {
      PrimaryIndexMapping indexMapping = PrimaryIndexes[0];
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
      JoinedTableRef = left;
      SqlJoinedTable joinedTable = left.InnerJoin(right, joinExpression);
      SetStatementFrom(statement, joinedTable);
    }

    #endregion

    protected QueryOperation(QueryProvider queryProvider)
      : base(queryProvider)
    {
    }
  }
}