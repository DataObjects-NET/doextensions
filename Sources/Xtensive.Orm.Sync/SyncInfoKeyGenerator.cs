using Xtensive.IoC;
using Xtensive.Orm.Configuration;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (KeyGenerator), Name = Name, Singleton = true)]
  public class SyncInfoKeyGenerator : CachingKeyGenerator<long>
  {
    public const string Name = "SyncInfo";

    private long lastTick;

    internal long GetLastTick()
    {
      if (lastTick > 0)
        return lastTick;

      return 0L;
    }

    internal long GetNextTick()
    {
      return GenerateKey(false).GetValue<long>(0);
    }

    public override Tuples.Tuple TryGenerateKey(bool temporaryKey)
    {
      var result = base.TryGenerateKey(temporaryKey);
      lastTick = result.GetValue<long>(0);
      return result;
    }

    // Constructors

    [ServiceConstructor]
    public SyncInfoKeyGenerator(DomainConfiguration configuration)
      : base(configuration)
    {
    }
  }
}