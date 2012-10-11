using Xtensive.Orm;
using Xtensive.Orm.Configuration;

namespace TestCommon
{
  public static class DomainConfigurationFactory
  {
     public static DomainConfiguration Create(string name = null)
     {
       var testConfiguration = TestConfiguration.Instance;
       var storageName = testConfiguration.Storage;
       if (!string.IsNullOrEmpty(name))
         storageName = string.Format("{0}_{1}", storageName, name);
       var configuration = DomainConfiguration.Load(storageName);
       configuration.UpgradeMode = DomainUpgradeMode.Recreate;
       var customConnectionInfo = testConfiguration.GetConnectionInfo(storageName);
       if (customConnectionInfo!=null)
         configuration.ConnectionInfo = customConnectionInfo;
       return configuration;
     }
  }
}