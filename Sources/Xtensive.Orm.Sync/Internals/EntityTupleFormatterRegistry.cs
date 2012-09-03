using System;
using System.Collections.Generic;
using System.Linq;

namespace Xtensive.Orm.Sync
{
  internal sealed class EntityTupleFormatterRegistry
  {
    private readonly Dictionary<Type, EntityTupleFormatter> tupleConverters;

    public EntityTupleFormatter Get(Type type)
    {
      if (type==null)
        throw new ArgumentNullException("type");
      return tupleConverters[type];
    }

    public EntityTupleFormatterRegistry(Domain domain)
    {
      tupleConverters = domain.Model.Types
        .Where(t => t.IsEntity)
        .Select(t => new EntityTupleFormatter(t))
        .ToDictionary(c => c.Type, c => c);
    }
  }
}