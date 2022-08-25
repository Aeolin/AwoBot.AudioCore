using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Utils
{
  // shamelessly copied from https://stackoverflow.com/questions/18756354/wrapping-manualresetevent-as-awaitable-task
  public static class WaitHandleExtensions
  {
    public static Task WaitOneAsync(this WaitHandle handle)
    {
      return WaitOneAsync(handle, Timeout.InfiniteTimeSpan);
    }

    public static Task WaitOneAsync(this WaitHandle handle, int timeout)
    {
      return handle.WaitOneAsync(TimeSpan.FromMilliseconds(timeout));
    }

    public static Task WaitOneAsync(this WaitHandle handle, TimeSpan timeout)
    {
      var tcs = new TaskCompletionSource();
      var registration = ThreadPool.RegisterWaitForSingleObject(handle, (state, timedOut) =>
      {
        var localTcs = (TaskCompletionSource)state;
        if (timedOut)
          localTcs.TrySetCanceled();
        else
          localTcs.TrySetResult();
      }, tcs, timeout, executeOnlyOnce: true);
      tcs.Task.ContinueWith((_, state) => ((RegisteredWaitHandle)state).Unregister(null), registration, TaskScheduler.Default);
      return tcs.Task;
    }
  }
}
