using AwoBot.AudioCore.Persistence;
using AwoBot.AudioCore.Playlists;
using AwoBot.AudioCore.Tracks;
using AwoBot.AudioCore.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Download
{
  public class DownloadManager
  {
    public event Action<ITrack> OnDownloadCancelled;
    public event Action<ITrack> OnDownloadCompleted;
    private readonly ITrackStorage _trackStore;
    private readonly Thread _downloadWorker;
    private readonly List<DownloadState> _states = new List<DownloadState>();
    private readonly ManualResetEvent _stateAddedTrigger = new ManualResetEvent(false);
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _queueLock = new SemaphoreSlim(1);

    public DownloadManager(ITrackStorage trackStore, ILoggerFactory loggerFactory)
    {
      _trackStore = trackStore;
      _downloadWorker = new Thread(threadWorker);
      _downloadWorker.Start();
      _logger = loggerFactory.CreateLogger<DownloadManager>();
    }

    private async Task<IStoredTrack> queueForDownloadAsync(ITrack track)
    {
      await _queueLock.WaitAsync(); // prevent the same track from being downloaded twice
      var stored = await _trackStore.GetOrCreateTrackAsync(track);
      if (stored.RequiresDownload)
      {
        if (_states.Any(x => x.Source.Equals(track)) == false)
        {
          var dlState = new DownloadState(track);
          if (await dlState.InitializeAsync() == false)
            return null;

          dlState.StartDownload(stored);
          _states.Add(dlState);
          _stateAddedTrigger.Set();
          await dlState.WaitForProgressAsync(Bytes.Mega(0.25));
        }
      }

      _queueLock.Release();
      return stored;
    }

    public async Task QueueForDownloadAsync(ITrack track)
    {
      await queueForDownloadAsync(track);
    }

    public async Task<Stream> OpenStreamAsync(ITrack track)
    {
      var stored = await queueForDownloadAsync(track);
      return stored.OpenRead();
    }

    private void removeState(DownloadState state)
    {
      _states.Remove(state);
      state.Dispose();
    }

    private async void threadWorker()
    {
      while (_cancellationTokenSource.IsCancellationRequested == false)
      {
        if (_states.Count > 0)
        {
          DownloadState state = null;
          try
          {
            var states = _states.ToArray();
            var index = Task.WaitAny(_states.Select(x => x.CurrentReadTask).ToArray());
            state = states[index];
            state.UpdateDownloadProcess();

            switch (state.State)
            {
              case State.Downloaded:
                await _trackStore.MarkDownloadedAsync(state.Target);
                removeState(state);
                OnDownloadCompleted?.Invoke(state.Source);
                _logger.LogInformation($"Finished downloading track: {state.Source}");
                break;

              case State.Cancelled:
                removeState(state);
                _logger.LogInformation($"Download for track {state.Source.Name} was cancelled");
                OnDownloadCancelled?.Invoke(state.Source);
                break;

              case State.Errored:
                removeState(state);
                _logger.LogError(state.CurrentReadTask.Exception, $"Error downloading track {state.Source.Name}");
                OnDownloadCancelled?.Invoke(state.Source);
                break;

              case State.Downloading:
                _logger.LogDebug($"[Progress] Track {state.Source.Name}: {Bytes.Format(state.BytesDownloaded, "0.00")} / {(state.BytesTotal.HasValue ? Bytes.Format(state.BytesTotal.Value, "0.00") : "???")} ({state.ProgressPercentage?.ToString("0.00") ?? "???"})%");
                break;
            }
          }
          catch (TaskCanceledException)
          {
            removeState(state);
            _logger.LogInformation("Download task was cancelled as expected");
          }
          catch (Exception ex)
          {
            removeState(state);
            OnDownloadCancelled?.Invoke(state.Source);
            _logger.LogError(ex, $"Error downloading Track {state.Source}");
          }
        }
        else
        {
          _logger.LogDebug("Waiting for queued downloads...");
          _stateAddedTrigger.WaitOne(5000);
          _stateAddedTrigger.Reset();
        }
      }

      if (_cancellationTokenSource.IsCancellationRequested)
        _states.ForEach(x => x.Cancel());
    }

  }
}
