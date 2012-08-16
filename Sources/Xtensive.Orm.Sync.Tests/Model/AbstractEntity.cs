using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xtensive.Orm.Sync.Tests.Model
{
  [HierarchyRoot]
  public abstract class AbstractEntity : Entity
  {
    [Field, Key]
    public int Id { get; private set; }

    [Field]
    public string Name { get; set; }

    protected AbstractEntity(Session session)
      : base(session)
    {
    }
  }
}
