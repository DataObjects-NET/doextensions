using System;
using TestCommon;
using Xtensive.Orm.Configuration;

namespace Xtensive.Orm.Sync.Tests
{
  public class SyncDomainManager : IDisposable
  {
    private Domain localDomain;
    private Domain remoteDomain;

    public Domain LocalDomain
    {
      get
      {
        if (localDomain==null)
          localDomain = BuildDomain();
        return localDomain;
      }
    }

    public Domain RemoteDomain
    {
      get
      {
        if (remoteDomain==null)
          remoteDomain = BuildDomain("remote");
        return remoteDomain;
      }
    }

    private Domain BuildDomain(string name = null)
    {
      var configuration = DomainConfigurationFactory.Create(name);
      configuration.Types.Register(typeof (SyncModule).Assembly);
      configuration.Types.Register(typeof (SyncDomainManager).Assembly);
      var domain = Domain.Build(configuration);
      domain.GetSyncManager().StartMetadataProcessor();
      return domain;
    }

    public void Dispose()
    {
      if (localDomain!=null) {
        try {
          localDomain.Dispose();
        }
        finally {
          localDomain = null;
        }
      }

      if (remoteDomain!=null) {
        try {
          remoteDomain.Dispose();
        }
        finally {
          remoteDomain = null;
        }
      }
    }
  }
}