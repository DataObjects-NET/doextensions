using Xtensive.Orm;
using Xtensive.Orm.Configuration;

namespace TestCommon
{
  public static class DomainConfigurationFactory
  {
     public static DomainConfiguration Create(string name = null)
     {
       var testConfiguration = TestConfiguration.Instance;
       var storageName = name ?? testConfiguration.Storage;
       var configuration = DomainConfiguration.Load(storageName);
       configuration.UpgradeMode = DomainUpgradeMode.Recreate;
       var customConnectionInfo = testConfiguration.GetConnectionInfo(storageName);
       if (customConnectionInfo!=null)
         configuration.ConnectionInfo = customConnectionInfo;
       return configuration;
     }
  }
}