using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Set of <see cref="ItemChange"/> items.
  /// </summary>
  [Serializable]
  public sealed class ChangeSet : IEnumerable<ItemChangeData>
  {
    private readonly Dictionary<Guid, ItemChangeData> index = new Dictionary<Guid,ItemChangeData>();

    /// <summary>
    /// Adds the specified data.
    /// </summary>
    /// <param name="data">The data.</param>
    public void Add(ItemChangeData data)
    {
      index[data.Identity.GlobalId] = data;
    }

    /// <summary>
    /// Gets the <see cref="Xtensive.Orm.Sync.ItemChangeData"/> with the specified global id.
    /// </summary>
    public ItemChangeData this[Guid globalId]
    {
      get
      {
        ItemChangeData result;
        index.TryGetValue(globalId, out result);
        return result;
      }
    }

    /// <summary>
    /// Gets the enumerator.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<ItemChangeData> GetEnumerator()
    {
      return index.Values.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>
    /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>
    /// Gets the item changes.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ItemChange> GetItemChanges()
    {
      return index.Values.Select(i => i.Change);
    }

    /// <summary>
    /// Binds all internal structures to <see cref="Domain"/>.
    /// </summary>
    /// <param name="domain">The domain.</param>
    public void BindTo(Domain domain)
    {
      foreach (var value in index.Values)
        value.BindTo(domain);
    }
  }
}
