using AwoBot.AudioCore.Persistence;
using AwoBot.AudioCore.Tracks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Download
{
  public class DownloadState 
  {
    public event Action<long, long> DownloadProgress;
    public ITrack Source { get; init; }
    public State State { get; private set; }
    public long BytesDownloaded { get; private set; }
    public long BytesTotal { get; private set; }

    private IStoredTrack _target;
    private readonly List<ProgressCompletionSource> _progressStates = new List<ProgressCompletionSource>();
    

    public Task WaitForProgressAsync(long progress, int? timeout = null)
    {
      var source = new ProgressCompletionSource(Math.Min(progress, BytesTotal), timeout);
      _progressStates.Add(source);
      return source.Task;
    } 

    public async Task PrepareForDownloadAsync()
    {
      if (State != State.Initialized)
        throw new InvalidOperationException("Can't initialize a download state again");

      if(await Source.TryOpenStreamAsync(out var stream, out var length))
      {
        BytesTotal = length;
      }
    }
  }

  public enum State
  {
    Initialized = 0,
    Started = 1,
    Downloading = 2,
    Downloaded = 3,
    Errored = 4,
    Canceled = 5
  }
}
