using System;

namespace Xtensive.Orm.Sync.Tests.Model
{
  [Serializable]
  [HierarchyRoot]
  public class MyEntity : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field(Length = 100)]
    public string Text { get; set; }

    [Field(Length = 100)]
    public string Text2 { get; set; }

    [Field]
    public DateTime Date { get; set; }

    [Field]
    public int Int { get; set; }

    [Field]
    public MyEnum Enum { get; set; }

    [Field]
    public MyStructure Structure { get; set; }

    [Field]
    public MyReferenceProperty Property { get; set; }

    [Field]
    [Association(OnOwnerRemove = OnRemoveAction.Cascade, OnTargetRemove = OnRemoveAction.Clear)]
    public EntitySet<MyEntitySetItem> Items { get; set; }

    public MyEntity(Session session)
      : base(session)
    {
    }
  }
}