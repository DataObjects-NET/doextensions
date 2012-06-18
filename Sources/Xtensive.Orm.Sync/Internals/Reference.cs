using Xtensive.Orm.Model;

namespace Xtensive.Orm.Sync
{
  public class Reference
  {
    public FieldInfo Field { get; private set; }

    public Identity Value { get; private set; }

    public Reference(FieldInfo field, Identity value)
    {
      Field = field;
      Value = value;
    }
  }
}
