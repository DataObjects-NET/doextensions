using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Graphs;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Sync
{
  internal class SyncRootSet : IEnumerable<SyncRoot>
  {
    private readonly DomainModel model;
    private List<SyncRoot> items;
    private Dictionary<Type, SyncRoot> index;

    public IEnumerator<SyncRoot> GetEnumerator()
    {
      return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    private void Initialize()
    {
      var graph = new Graph<Node<SyncRoot>, Edge<FieldInfo>>();
      var nodeIndex = new Dictionary<Type, Node<SyncRoot>>();

      var types = model.Types[typeof(SyncInfo)].GetDescendants();
      foreach (var type in types) {
        var entityField = type.Fields["Entity"];
        var syncRoot = new SyncRoot {
          EntityField = entityField,
          ItemType = type.UnderlyingType
        };
        var node = new Node<SyncRoot>(syncRoot);
        nodeIndex[syncRoot.EntityType] = node;
        graph.Nodes.Add(node);
      }

      foreach (var left in nodeIndex.Values) {
        var keyFields = model.Types[left.Value.EntityType].Hierarchy.Key.Fields;
        foreach (var field in keyFields) {
          if (!field.IsEntity)
            continue;
          var right = nodeIndex[model.Types[field.ValueType].GetRoot().UnderlyingType];
          new Edge<FieldInfo>(right, left, field).Attach();
        }
      }

      var result = TopologicalSorter.Sort(graph);
      items = result.SortedNodes.Select(n => n.Value).ToList();
      index = items.ToDictionary(i => i.EntityType);
    }

    public SyncRoot this[Type entityType]
    {
      get
      {
        SyncRoot result;
        if (index.TryGetValue(entityType, out result))
          return result;

        var rootType = model.Types[entityType].Hierarchy.Root.UnderlyingType;
        index.TryGetValue(rootType, out result);
        return result;
      }
    }

    public SyncRootSet(DomainModel model)
    {
      this.model = model;
      Initialize();
    }
  }
}
