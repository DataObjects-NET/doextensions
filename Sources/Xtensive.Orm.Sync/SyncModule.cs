using System.Collections.Generic;
using System.Linq;
using Xtensive.Orm.Building;
using Xtensive.Orm.Sync.Model;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// <see cref="IModule"/> implementation for sync extension.
  /// </summary>
  public sealed class SyncModule : Module
  {
    /// <summary>
    /// Called when 'complex' build process is completed.
    /// </summary>
    /// <param name="domain">The built domain.</param>
    public override void OnBuilt(Domain domain)
    {
      // This ensures that sync manager is subscribed to all required events
      // before executing any user code.

      domain.GetSyncManager();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="autoGenerics"></param>
    public override void OnAutoGenericsBuilt(BuildingContext context, ICollection<System.Type> autoGenerics)
    {
      var thisAssembly = GetType().Assembly;
      var toRemove = autoGenerics
        .Where(t => t.GetGenericTypeDefinition()==typeof (SyncInfo<>) && t.GetGenericArguments()[0].Assembly==thisAssembly)
        .ToList();

      foreach (var type in toRemove)
        autoGenerics.Remove(type);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncModule"/> class.
    /// </summary>
    public SyncModule()
    {
    }
  }
}
