using System.Linq;
using Xtensive.Orm.Building;
using Xtensive.Orm.Building.Definitions;
using Xtensive.Orm.Tracking;

namespace Xtensive.Orm.Sync
{
  public class SyncModule : IModule
  {
    private Domain domain;

    public void OnDefinitionsBuilt(BuildingContext context, DomainModelDef model)
    {
    }

    public void OnBuilt(Domain domain)
    {
      this.domain = domain;
      var m = domain.Services.Get<IDomainTrackingMonitor>();
      m.TrackingCompleted += OnTrackingCompleted;
    }

    private void OnTrackingCompleted(object sender, TrackingCompletedEventArgs e)
    {
      var changes = e.Result.GetChanges();
      if (!changes.Any())
        return;

      var items = changes
        .Where(TrackingItemFilter)
        .ToList();

      if (items.Count == 0)
        return;

      using (var session = domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        var ms = session.Services.Get<SyncMetadataStore>();
        ms.ProcessTrackingResult(items);
        t.Complete();
      }
    }

    private static bool TrackingItemFilter(ITrackingItem item)
    {
      var entityKey = item.Key;
      var entityType = entityKey.TypeInfo.UnderlyingType;

      if (entityType.Assembly==typeof (Persistent).Assembly)
        return false;
      if (entityType.Assembly==typeof (SyncInfo).Assembly)
        return false;

      return true;
    }
  }
}
