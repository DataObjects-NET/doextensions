using System;
using System.Linq;
using Xtensive.Core;
using Xtensive.Orm.Model;
using Xtensive.Tuples;
using Xtensive.Tuples.Transform;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Sync
{
  internal sealed class CanonicalTupleConverter
  {
    public Type Type { get; private set; }

    private readonly Tuple canonicalTuplePrototype;
    private readonly Tuple domainTuplePrototype;

    private readonly int[] domainToCanonicalMap;
    private readonly int[] canonicalToDomainMap;

    public Tuple GetCanonicalTuple(Tuple tuple)
    {
      var result = canonicalTuplePrototype.CreateNew();
      tuple.CopyTo(result, domainToCanonicalMap);
      return result;
    }

    public Tuple GetDomainTuple(Tuple tuple)
    {
      var result = domainTuplePrototype.CreateNew();
      tuple.CopyTo(result, canonicalToDomainMap);
      return result;
    }

    public CanonicalTupleConverter(TypeInfo type)
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
      canonicalTuplePrototype = Tuple.Create(domainToCanonicalMap.Select(i => domainTupleDescriptor[i]).ToArray());
    }
  }
}