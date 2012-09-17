using Xtensive.Orm.Building;
using Xtensive.Orm.Building.Definitions;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// <see cref="IModule"/> implementation for sync extension.
  /// </summary>
  public sealed class SyncModule : IModule
  {
    /// <summary>
    /// Called when the build of <see cref="T:Xtensive.Orm.Building.Definitions.DomainModelDef"/> is completed.
    /// </summary>
    /// <param name="context">The domain building context.</param>
    /// <param name="model">The domain model definition.</param>
    public void OnDefinitionsBuilt(BuildingContext context, DomainModelDef model)
    {
    }

    /// <summary>
    /// Called when 'complex' build process is completed.
    /// </summary>
    /// <param name="domain">The built domain.</param>
    public void OnBuilt(Domain domain)
    {
      // This ensures that sync manager is subscribed to all required events
      // before executing any user code.

      domain.GetSyncManager();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncModule"/> class.
    /// </summary>
    public SyncModule()
    {
    }
  }
}
