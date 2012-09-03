using System;
using System.Linq;
using Xtensive.Orm.Model;
using Xtensive.Tuples;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Sync
{
  internal sealed class EntityTupleFormatter
  {
    public Type Type { get; private set; }

    private readonly Tuple canonicalTuplePrototype;
    private readonly Tuple domainTuplePrototype;

    private readonly int[] domainToCanonicalMap;
    private readonly int[] canonicalToDomainMap;

    public string Format(Tuple domainTuple)
    {
      if (domainTuple==null)
        return null;

      var canonicalTuple = canonicalTuplePrototype.CreateNew();
      domainTuple.CopyTo(canonicalTuple, canonicalToDomainMap);
      return canonicalTuple.Format();
    }

    public Tuple Parse(string value)
    {
      if (value==null)
        return null;

      var canonicalTuple = canonicalTuplePrototype.Descriptor.Parse(value);
      var domainTuple = domainTuplePrototype.CreateNew();
      canonicalTuple.CopyTo(domainTuple, domainToCanonicalMap);
      return domainTuple;
    }

    public EntityTupleFormatter(TypeInfo type)
    {
      Type = type.UnderlyingType;

      var fixedFields = type.Fields
        .Where(f => f.IsPrimitive && (f.IsPrimaryKey || f.IsTypeId))
        .Select(f => new {
          DomainIndex = f.MappingInfo.Offset,
          CanonicalIndex = f.MappingInfo.Offset
        })
        .ToList();

      var valueFields = type.Fields
        .Where(f => f.IsPrimitive && !(f.IsPrimaryKey || f.IsTypeId))
        .OrderBy(f => f.Name, StringComparer.InvariantCulture)
        .Select((f, i) => new {
          DomainIndex = f.MappingInfo.Offset,
          CanonicalIndex = i + fixedFields.Count
        });

      var fieldMap = fixedFields.Concat(valueFields).ToList();

      domainToCanonicalMap = fieldMap
        .OrderBy(i => i.DomainIndex)
        .Select(i => i.CanonicalIndex)
        .ToArray();

      canonicalToDomainMap = fieldMap
        .OrderBy(i => i.CanonicalIndex)
        .Select(i => i.DomainIndex)
        .ToArray();

      domainTuplePrototype = type.TuplePrototype;
      var domainTupleDescriptor = type.TupleDescriptor;
      canonicalTuplePrototype = Tuple.Create(canonicalToDomainMap.Select(i => domainTupleDescriptor[i]).ToArray());
    }
  }
}