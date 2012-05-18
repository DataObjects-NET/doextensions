using Xtensive.Orm.Model;

namespace Xtensive.Orm.Tracking
{
  public class ChangedValue
  {
    public FieldInfo Field { get; private set; }

    public object OriginalValue { get; private set; }

    public object NewValue { get; private set; }

    public ChangedValue(FieldInfo field, object originalValue, object newValue)
    {
      Field = field;
      OriginalValue = originalValue;
      NewValue = newValue;
    }
  }
}
