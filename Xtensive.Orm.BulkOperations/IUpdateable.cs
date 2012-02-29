using Xtensive.Orm.BulkOperations;

namespace Xtensive.Orm
{
  /// <summary>
  /// Contains UPDATE operation data. You need run operation via <see cref="BulkExtensions.Update{T}(IUpdateable{T})"/> method or you can <see cref="BulkExtensions.Set{T,TResult}(IUpdateable{T},System.Linq.Expressions.Expression{System.Func{T,TResult}},System.Linq.Expressions.Expression{System.Func{T,TResult}})"/> another field.
  /// </summary>
  /// <typeparam name="T">Type of the entity.</typeparam>
  public interface IUpdateable<T>
  {
  }
}