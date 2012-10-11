using Xtensive.Orm.Model;
using Xtensive.Orm.Sync.DataExchange;

namespace Xtensive.Orm.Sync
{
  internal sealed class KeyDependency
  {
    public EntityState Target { get; private set; }

    public FieldInfo Field { get; private set; }

    public Identity OriginalValue { get; private set; }

    public KeyDependency(EntityState target, FieldInfo field, Identity value)
    {
      Target = target;
      Field = field;
      OriginalValue = value;
    }
  }
}
