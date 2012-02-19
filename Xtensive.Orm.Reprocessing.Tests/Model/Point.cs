namespace Xtensive.Orm.Reprocessing.Tests.Model
{
  public class Point : Structure
  {
    [Field]
    public int? X { get; set; }

    [Field]
    public int? Y { get; set; }
  }
}
