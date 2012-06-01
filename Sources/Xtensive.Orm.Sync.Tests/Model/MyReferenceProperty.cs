namespace Xtensive.Orm.Sync.Tests.Model
{
  [HierarchyRoot]
  public class MyReferenceProperty : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field]
    public string Name { get; set; }

    public MyReferenceProperty()
    {
    }

    public MyReferenceProperty(Session session)
      : base(session)
    {
    }
  }
}