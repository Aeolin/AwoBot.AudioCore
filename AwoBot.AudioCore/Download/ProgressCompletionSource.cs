using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Download
{
  public class ProgressCompletionSource
  {
    private TaskCompletionSource _tcs = new TaskCompletionSource();
    private Timer _timer;
    private long _progress;
    public Task Task => _tcs.Task;
    public bool Completered => _tcs.Task.IsCompleted;

    public ProgressCompletionSource(long progress, int? timeout = 5000)
    {
      _progress = progress;
      if (timeout.HasValue && timeout.Value > Timeout.Infinite)
        _timer = new Timer(onTimeout, null, timeout.Value, Timeout.Infinite);
    }

    private void onTimeout(object _)
    {
      _tcs.SetException(new TimeoutException("Timed out"));
    }

    public void NotifyProgress(long progress, long max)
    {
      if (progress > _progress)
        _tcs.SetResult();
    }

  }
}
