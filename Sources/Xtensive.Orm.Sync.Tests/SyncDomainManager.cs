using System;
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
          localDomain = BuildDomain("local");
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

    private Domain BuildDomain(string configuration)
    {
      var domain = Domain.Build(DomainConfiguration.Load(configuration));
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