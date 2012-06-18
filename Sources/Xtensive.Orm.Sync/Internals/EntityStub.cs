using System;
using System.Collections.Generic;

namespace Xtensive.Orm.Sync
{
  public class EntityStub
  {
    public EntityState State { get; set; }

    public IDisposable Pin { get; private set; }

    public List<Reference> References { get; private set; }

    public EntityStub(EntityState entity, IDisposable pin)
    {
      State = entity;
      Pin = pin;
      References = new List<Reference>();
    }
  }
}
