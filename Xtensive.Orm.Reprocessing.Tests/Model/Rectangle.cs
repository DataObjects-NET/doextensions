namespace Xtensive.Orm.Reprocessing.Tests.Model
{
  public class Rectangle : Structure
  {
    [Field]
    public int? BorderWidth { get; set; }
    [Field]
    public Point First { get; set; }
    [Field]
    public Point Second { get; set; }
  }
}
