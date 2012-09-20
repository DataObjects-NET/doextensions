using Xtensive.Orm;

namespace TestCommon.Model
{
  [HierarchyRoot]
  [Index("Name", Unique = true)]
  [KeyGenerator(KeyGeneratorKind.None)]
  public class Foo2 : Entity
  {

    public Foo2(Session session, decimal id, string id2)
      : base(session, id, id2)
    {
    }

    [Field]
    [Key(0)]
    public decimal Id { get; set; }

    [Field]
    [Key(1)]
    public string Id2 { get; set; }

    [Field]
    public string Name { get; set; }

    [Field]
    [Association("Foo")]
    public Bar2 Bar { get; set; }
  }
}
