namespace Xtensive.Orm.Sync.Tests
{
  public abstract class AutoBuildTestWithRollback : AutoBuildTest
  {
    #region Setup/Teardown

    public override void TestSetUp()
    {
      LocalSession = LocalDomain.OpenSession();
      LocalTransactionScope = LocalSession.OpenTransaction();
      RemoteSession = RemoteDomain.OpenSession();
      RemoteTransactionScope = RemoteSession.OpenTransaction();
    }

    public override void TestTearDown()
    {
      LocalTransactionScope.Dispose();
      LocalSession.Dispose();
      RemoteTransactionScope.Dispose();
      RemoteSession.Dispose();
    }

    #endregion

    protected Session LocalSession { get; private set; }
    protected TransactionScope LocalTransactionScope { get; private set; }
    protected Session RemoteSession { get; private set; }
    protected TransactionScope RemoteTransactionScope { get; private set; }
  }
}