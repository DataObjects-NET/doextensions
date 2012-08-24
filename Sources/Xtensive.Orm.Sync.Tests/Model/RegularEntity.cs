using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xtensive.Orm.Sync.Tests.Model
{
  public class RegularEntity : AbstractEntity
  {
    public RegularEntity(Session session)
      : base(session)
    {
    }
  }
}
