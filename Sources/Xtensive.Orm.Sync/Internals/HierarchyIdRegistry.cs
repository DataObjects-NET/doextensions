using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Xtensive.IoC;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (HierarchyIdRegistry), Singleton = true)]
  internal sealed class HierarchyIdRegistry : IDomainService
  {
    private readonly Dictionary<TypeInfo, int> typeToIdMap = new Dictionary<TypeInfo, int>();
    private readonly Dictionary<int, TypeInfo> idToTypeMap = new Dictionary<int, TypeInfo>();

    public int GetHierarchyId(TypeInfo typeInfo)
    {
      return typeToIdMap[typeInfo];
    }

    public TypeInfo GetHierarchyRoot(int id)
    {
      return idToTypeMap[id];
    }

    private static int BuildHierarchyId(Type type)
    {
      var attributes = type.GetCustomAttributes(typeof (HierarchyIdAttribute), false);
      if (attributes.Length > 0)
        return ((HierarchyIdAttribute) attributes[0]).HierarchyId;

      var nameBytes = Encoding.UTF8.GetBytes(type.Name);

      byte[] hash;

      using (var provider = new SHA1Managed()) {
        provider.ComputeHash(nameBytes);
        hash = provider.Hash;
      }

      return hash[0] | (hash[1] << 8) | (hash[2] << 16) | (hash[3] << 24);
    }

    [ServiceConstructor]
    public HierarchyIdRegistry(Domain domain)
    {
      foreach (var hierarchy in domain.Model.Hierarchies) {
        var root = hierarchy.Root;
        var id = BuildHierarchyId(root.UnderlyingType);
        typeToIdMap.Add(root, id);
        idToTypeMap.Add(id, root);
      }
    }
  }
}