=================
Xtensive.Orm.Sync
=================

Summary
-------
The extension implements KnowledgeSyncProvider and all the required infrastructure for Microsoft Sync framework support.

Prerequisites
-------------
DataObjects.Net 4.6 or later (http://dataobjects.net)

Implementation
--------------
1. Add reference to Microsoft.Synchronization, Xtensive.Orm.Sync and Xtensive.Orm.Tracking assemblies
2. Include types from Xtensive.Orm.Sync and Xtensive.Orm.Tracking assemblies into the domain:

  <Xtensive.Orm>
    <domains>
      <domain ... >
        <types>
          <add assembly="your assembly"/>
          <add assembly="Xtensive.Orm.Tracking"/>
          <add assembly="Xtensive.Orm.Sync"/>
        </types>
      </domain>
    </domains>
  </Xtensive.Orm>

3. During Domain build process the extension will automatically register SyncInfo<TEntity> type and that 
will produce the corresponding auxiliary type for every hierarchy root. e.g. SyncInfo<Person>, SyncInfo<Animal>, etc. 
These types are used to store information specific for MS Sync framework and is known as sync metadata. 
They collect information about entity creation, modification, removal and expose that data during synchronization session.


Demo
----
Synchronization is done for 2 domains: they are called local and remote.

1. Full synchronization:

var localDomain = BuildDomain(localDomainConfiguration);
var remoteDomain = BuildDomain(remoteDomainConfiguration);

var orchestrator = new SyncOrchestrator {
    LocalProvider = localDomain.GetSyncProvider(),
    RemoteProvider = remoteDomain.GetSyncProvider(),
    Direction = SyncDirectionOrder.Upload
};
orchestrator.Synchronize();

2. Partial synchronization:

var localDomain = BuildDomain(localDomainConfiguration);
var remoteDomain = BuildDomain(remoteDomainConfiguration);

var localProvider = LocalDomain.GetSyncProvider();

// Fluent configuration of what exactly should be synchronized
localProvider.Sync
    .All<Person>()
    .All<Animal>(a => a.HasName)
    .Skip<Insect>();

var orchestrator = new SyncOrchestrator {
    LocalProvider = localProvider,
    RemoteProvider = remoteDomain.GetSyncProvider(),
    Direction = SyncDirectionOrder.Upload
};
orchestrator.Synchronize();

References
----------
http://doextensions.codeplex.com