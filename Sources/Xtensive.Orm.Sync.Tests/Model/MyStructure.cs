namespace Xtensive.Orm.Sync.Tests.Model
{
  public class MyStructure : Structure
  {
    [Field]
    public MyEntitySubStructure SubStructure { get; set; }

    [Field]
    public int Z { get; set; }

    public override string ToString()
    {
      return string.Format("MyStructure: Z={0}, SubStructure=({1})", Z, SubStructure);
    }

    public MyStructure()
    {
    }

    public MyStructure(Session session)
      : base(session)
    {
    }
  }

  public class MyEntitySubStructure : Structure
  {
    [Field]
    public int X { get; set; }

    [Field]
    public int Y { get; set; }

    public override string ToString()
    {
      return string.Format("MySubStructure: X={0}, Y={1}", X, Y);
    }

    public MyEntitySubStructure()
    {
    }

    public MyEntitySubStructure(Session session)
      : base(session)
    {
    }
  }
}