using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Sync
{
  internal sealed class CanonicalTupleConverterRegistry
  {
    private readonly Dictionary<Type, CanonicalTupleConverter> tupleConverters;

    public CanonicalTupleConverter GetConverter(Type type)
    {
      if (type==null)
        throw new ArgumentNullException("type");
      return tupleConverters[type];
    }

    public CanonicalTupleConverterRegistry(Domain domain)
    {
      tupleConverters = domain.Model.Types
        .Where(t => t.IsEntity)
        .Select(t => new CanonicalTupleConverter(t))
        .ToDictionary(c => c.Type, c => c);
    }
  }
}