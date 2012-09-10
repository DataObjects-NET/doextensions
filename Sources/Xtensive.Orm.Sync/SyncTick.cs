namespace Xtensive.Orm.Sync
{
  [HierarchyRoot, KeyGenerator(Name = WellKnown.TickGeneratorName)]
  internal abstract class SyncTick : Entity
  {
    [Key, Field]
    public long Id { get; private set; }
  }
}