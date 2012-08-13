using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xtensive.Orm.Sync.Tests.Model
{
  [KeyGenerator(KeyGeneratorKind.None)]
  [HierarchyRoot]
  public class AnotherEntity : Entity
  {
    [Field, Key]
    public Guid Id { get; set; }

    [Field]
    public string Text { get; set; }

    public AnotherEntity(Session session, Guid id)
      : base(session, id)
    {
    }
  }
}
