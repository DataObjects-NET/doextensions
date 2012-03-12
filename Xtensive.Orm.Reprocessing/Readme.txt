Xtensive.Orm.Reprocessing extension
===================================

Overview
--------
The extension provides API for reprocessible operations. The reprocessible operation 
should represent a separate block of logic, usually a delegate of a method and be transactional.

Prerequisites
-------------
DataObjects.Net 4.5 or later (http://dataobjects.net)

Configuration
-------------
1. Add reference to Xtensive.Orm.Reprocessing assembly

How to use
----------
1. Simple reprocessible operation looks like this:

Domain.Execute(session =>
  {
    // Task logic
  });

2. There are 3 strategies that can be used for task execution:
- HandleReprocessibleException strategy
    The strategy catches all reprocessible expections (deadlock and transaction serialization exceptions)
    and makes another attempt to execute the task
- HandleUniqueConstraintViolation strategy
    The same as previous one but also catches unique constraint violation exception 
- NoReprocess strategy
    No reprocessing is provided

To indicate that a particular strategy chould be applied, use the following syntax:

Domain.WithStrategy(new HandleReprocessExceptionStrategy())
      .Execute(session =>
          {
            // Task logic
          });

3. To omit setting up the strategy each time you might want to configure it in 
application configuration file, e.g.:

<configSections>
    ...
    <section name="Xtensive.Orm.Reprocessing" type="Xtensive.Orm.Reprocessing.Configuration.ConfigurationSection, Xtensive.Orm.Reprocessing" />
  </configSections>

  <Xtensive.Orm.Reprocessing defaultTransactionOpenMode="New" defaultExecuteStrategy="Xtensive.Orm.Reprocessing.HandleReprocessableExceptionStrategy, Xtensive.Orm.Reprocessing">
  </Xtensive.Orm.Reprocessing>


More information
----------------
http://doextensions.codeplex.com