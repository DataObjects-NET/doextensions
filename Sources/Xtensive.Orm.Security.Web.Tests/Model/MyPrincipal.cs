namespace Xtensive.Orm.Security.Web.Tests.Model
{
  [HierarchyRoot]
  public class MyPrincipal : MembershipPrincipal
  {
    [Field, Key]
    public int Id { get; set; }

    public MyPrincipal(Session session)
      : base(session)
    {
    }

    public MyPrincipal()
    {
    }
  }
}