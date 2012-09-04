using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.IoC;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (EntityTupleFormatterRegistry), Singleton = true)]
  internal sealed class EntityTupleFormatterRegistry : IDomainService
  {
    private readonly Dictionary<Type, EntityTupleFormatter> tupleConverters;

    public EntityTupleFormatter Get(Type type)
    {
      if (type==null)
        throw new ArgumentNullException("type");
      return tupleConverters[type];
    }

    [ServiceConstructor]
    public EntityTupleFormatterRegistry(Domain domain)
    {
      tupleConverters = domain.Model.Types
        .Where(t => t.IsEntity)
        .Select(t => new EntityTupleFormatter(t))
        .ToDictionary(c => c.Type, c => c);
    }
  }
}