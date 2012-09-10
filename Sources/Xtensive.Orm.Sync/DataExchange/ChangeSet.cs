using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync.DataExchange
{
  /// <summary>
  /// Set of <see cref="ItemChange"/> items.
  /// </summary>
  [Serializable]
  public sealed class ChangeSet : IEnumerable<ItemChangeData>
  {
    private readonly Dictionary<SyncId, ItemChangeData> index = new Dictionary<SyncId, ItemChangeData>();

    /// <summary>
    /// Gets the <see cref="ItemChangeData"/> with the specified global id.
    /// </summary>
    public ItemChangeData this[SyncId globalId]
    {
      get
      {
        ItemChangeData result;
        index.TryGetValue(globalId, out result);
        return result;
      }
    }

    /// <summary>
    /// Gets number of <see cref="ItemChangeData"/> in this instance.
    /// </summary>
    public int Count
    {
      get { return index.Count; }
    }

    /// <summary>
    /// Adds the specified data.
    /// </summary>
    /// <param name="data">The data.</param>
    public void Add(ItemChangeData data)
    {
      index[data.Identity.GlobalId] = data;
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

    /// <summary>
    /// Initializes new instance of <see cref="ChangeSet"/> class.
    /// </summary>
    public ChangeSet()
    {
    }
  }
}
