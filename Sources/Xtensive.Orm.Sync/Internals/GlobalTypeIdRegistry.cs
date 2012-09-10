using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Xtensive.IoC;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (GlobalTypeIdRegistry), Singleton = true)]
  internal sealed class GlobalTypeIdRegistry : IDomainService
  {
    private readonly ConcurrentDictionary<Type, int> cache = new ConcurrentDictionary<Type, int>();

    public int GetGlobalTypeId(Type type)
    {
      if (type==null)
        throw new ArgumentNullException("type");
      return cache.GetOrAdd(type, ReadGlobalTypeId);
    }

    private int ReadGlobalTypeId(Type type)
    {
      var attributes = type.GetCustomAttributes(typeof (GlobalTypeIdAttribute), false);
      if (attributes.Length > 0)
        return ((GlobalTypeIdAttribute) attributes[0]).GlobalTypeId;

      var nameBytes = Encoding.UTF8.GetBytes(type.Name);

      byte[] hash;

      using (var provider = new SHA1Managed()) {
        provider.ComputeHash(nameBytes);
        hash = provider.Hash;
      }

      return hash[0] | (hash[1] << 8) | (hash[2] << 16) | (hash[3] << 24);
    }

    [ServiceConstructor]
    public GlobalTypeIdRegistry(Domain domain)
    {
    }
  }
}