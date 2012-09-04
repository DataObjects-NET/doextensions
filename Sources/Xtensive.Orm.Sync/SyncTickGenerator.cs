using System;
using System.Linq;
using Xtensive.IoC;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// <see cref="IKeyGenerator"/> wrapper
  /// </summary>
  [Service(typeof(SyncTickGenerator), Singleton = true)]
  public class SyncTickGenerator : IDomainService
  {
    private readonly IKeyGenerator generator;
    private readonly KeyInfo keyInfo;
    private long lastTick;

    /// <summary>
    /// Gets the last tick.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <returns></returns>
    public long GetLastTick(Session session)
    {
      if (lastTick > 0)
        return lastTick;

      lastTick = session.Query.All<SyncInfo>()
        .Where(i => i.ChangeReplicaKey==0)
        .Max(i => i.ChangeTickCount);

      return lastTick;
    }

    /// <summary>
    /// Gets the next tick.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <returns></returns>
    public long GetNextTick(Session session)
    {
      lastTick = generator.GenerateKey(keyInfo, session).GetValue<long>(0);
      return lastTick;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncTickGenerator"/> class.
    /// </summary>
    /// <param name="domain">The domain.</param>
    [ServiceConstructor]
    public SyncTickGenerator(Domain domain)
    {
      keyInfo = domain.Model.Types[typeof (SyncInfo)].Key;
      generator = domain.Services.Get<IKeyGenerator>(Wellknown.TickGeneratorName);
    }
  }
}
