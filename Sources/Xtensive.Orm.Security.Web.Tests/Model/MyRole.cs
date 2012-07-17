namespace Xtensive.Orm.Security.Web.Tests.Model
{
  [HierarchyRoot]
  public class MyRole : Role
  {
    [Field, Key]
    public int Id { get; set; }

    public MyRole(Session session)
      : base(session)
    {
    }
  }
}