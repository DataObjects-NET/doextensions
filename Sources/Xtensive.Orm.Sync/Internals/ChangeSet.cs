using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  internal class ChangeSet : IEnumerable<ItemChangeData>
  {
    private readonly Dictionary<Guid, ItemChangeData> index = new Dictionary<Guid,ItemChangeData>();

    public void Add(ItemChangeData data)
    {
      index[data.Identity.GlobalId] = data;
    }

    public ItemChangeData this[Guid globalId]
    {
      get
      {
        ItemChangeData result;
        index.TryGetValue(globalId, out result);
        return result;
      }
    }

    public IEnumerator<ItemChangeData> GetEnumerator()
    {
      return index.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public IEnumerable<ItemChange> GetItemChanges()
    {
      return index.Values.Select(i => i.Change);
    }
  }
}
