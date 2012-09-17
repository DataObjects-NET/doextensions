using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Xtensive.Orm.Sync
{
  internal sealed class MetadataProcessor : IDisposable
  {
    private readonly Domain domain;
    private readonly Task processorTask;

    private readonly ManualResetEvent idle = new ManualResetEvent(true);
    private readonly ManualResetEvent aborted = new ManualResetEvent(false);
    private readonly AutoResetEvent dataAvailable = new AutoResetEvent(true);

    public void NotifyDataAvailable()
    {
      idle.Reset();
      dataAvailable.Set();
    }

    public void WaitForIdle()
    {
      idle.WaitOne();
    }

    public void Dispose()
    {
      aborted.Set();
      processorTask.Wait();

      aborted.Dispose();
      idle.Dispose();
      dataAvailable.Dispose();
      processorTask.Dispose();
    }

    private void MaintainSyncLog()
    {
      var done = false;
      while (!done) {
        idle.Reset();
        try {
          while (!done && MetadataUpdater.MaintainSyncLogOnce(domain))
            done = CheckForAbort();
        }
        catch(Exception exception) {
          Debug.Write(exception.ToString());
        }
        idle.Set();
        done = WaitForAbortOrDataAvailable();
      }
    }

    private bool CheckForAbort()
    {
      return aborted.WaitOne(0);
    }

    private bool WaitForAbortOrDataAvailable()
    {
      return WaitHandle.WaitAny(new WaitHandle[] {aborted, dataAvailable}, TimeSpan.FromSeconds(3))==0;
    }

    public MetadataProcessor(Domain domain)
    {
      if (domain==null)
        throw new ArgumentNullException("domain");
      this.domain = domain;

      processorTask = Task.Factory.StartNew(MaintainSyncLog);
    }
  }
}