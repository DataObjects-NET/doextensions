﻿using System;
using System.Linq;
using Xtensive.IoC;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// <see cref="IKeyGenerator"/> wrapper
  /// </summary>
  [Service(typeof (SyncTickGenerator), Singleton = true)]
  internal sealed class SyncTickGenerator : ISessionService
  {
    private readonly Session session;
    private readonly IKeyGenerator generator;
    private readonly KeyInfo keyInfo;

    private long lastTick = -1;

    /// <summary>
    /// Gets the last tick.
    /// </summary>
    /// <returns>Last tick in current domain.</returns>
    public long GetLastTick()
    {
      if (lastTick < 0)
        lastTick = FetchLastTick();
      return lastTick;
    }

    /// <summary>
    /// Gets the next tick.
    /// </summary>
    /// <returns>Next tick in current domain.</returns>
    public long GetNextTick()
    {
      return lastTick = generator.GenerateKey(keyInfo, session).GetValue<long>(0);
    }

    private long FetchLastTick()
    {
      return session.Query.Execute(
        q => q.All<SyncInfo>().Where(i => i.ChangeReplicaKey==0).Max(i => i.ChangeTickCount));
    }

    private void OnTransactionCompleted(object sender, TransactionEventArgs e)
    {
      lastTick = -1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncTickGenerator"/> class.
    /// </summary>
    /// <param name="session">The session</param>
    [ServiceConstructor]
    public SyncTickGenerator(Session session)
    {
      if (session==null)
        throw new ArgumentNullException("session");

      this.session = session;

      keyInfo = session.Domain.Model.Types[typeof (SyncInfo)].Key;
      generator = session.Domain.Services.Get<IKeyGenerator>(WellKnown.TickGeneratorName);

      session.Events.TransactionCommitted += OnTransactionCompleted;
      session.Events.TransactionRollbacked += OnTransactionCompleted;
    }
  }
}
