namespace Xtensive.Orm.Sync.Tests.Model
{
  [HierarchyRoot]
  public class MyEntitySetItem : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field(Length = 100)]
    public string ItemText { get; set; }

    public MyEntitySetItem()
    {
    }

    public MyEntitySetItem(Session session)
      : base(session)
    {
    }
  }
}