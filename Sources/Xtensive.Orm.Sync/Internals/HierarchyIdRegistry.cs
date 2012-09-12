using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Xtensive.IoC;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (HierarchyIdRegistry), Singleton = true)]
  internal sealed class HierarchyIdRegistry : IDomainService
  {
    private readonly ConcurrentDictionary<Type, int> cache = new ConcurrentDictionary<Type, int>();

    public int GetHierarchyId(TypeInfo typeInfo)
    {
      if (typeInfo==null)
        throw new ArgumentNullException("typeInfo");
      return cache.GetOrAdd(typeInfo.Hierarchy.Root.UnderlyingType, ReadHierarchyId);
    }

    private int ReadHierarchyId(Type type)
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
    }
  }
}