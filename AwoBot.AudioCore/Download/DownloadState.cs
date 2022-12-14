using AwoBot.AudioCore.Persistence;
using AwoBot.AudioCore.Tracks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Download
{
  public class DownloadState : IDisposable
  {
    public event Action<ITrack, long, long> OnDownloadProgress;
    public ITrack Source { get; init; }
    public IStoredTrack Target { get; private set; }
    public State State { get; private set; }
    public long BytesDownloaded { get; private set; }
    public long? BytesTotal { get; private set; }
    public Task<int> CurrentReadTask { get; private set; }
    public int Priority { get; set; } = 1;
    public Exception Exception { get; private set; }
    public double? ProgressPercentage => BytesTotal.HasValue ? BytesDownloaded / ((double)BytesTotal.Value) * 100 : null;

    private CancellationTokenSource _cancellationToken;
    private byte[] _downloadBuffer;
    private Stream _targetStream;
    private Stream _sourceStream;
    private HttpClient _httpClient;

    private readonly List<ProgressCompletionSource> _progressStates = new List<ProgressCompletionSource>();


    public DownloadState(ITrack track)
    {
      Source = track;
      _cancellationToken = new CancellationTokenSource();
    }


    public void UpdateDownloadProcess()
    {
      _cancellationToken.Token.ThrowIfCancellationRequested();
      if (CurrentReadTask?.IsCompletedSuccessfully == true)
      {
        var len = CurrentReadTask.Result;
        if (len == 0)
        {
          State = State.Downloaded;
          Dispose();
          return;
        }
        else
        {
          _targetStream.Write(_downloadBuffer, 0, len);
          BytesDownloaded += len;
          updateProgressWaiters();
          OnDownloadProgress?.Invoke(Source, BytesDownloaded, BytesTotal ?? -1);
          CurrentReadTask = _sourceStream.ReadAsync(_downloadBuffer, 0, _downloadBuffer.Length, _cancellationToken.Token);
        }
      }
      else if (CurrentReadTask?.IsFaulted == true)
      {
        Exception = CurrentReadTask.Exception;
        State = State.Errored;
        Dispose();
        return;
      }
    }

    private void updateProgressWaiters()
    {
      _progressStates.RemoveAll(x => x.Completed);
      _progressStates.ForEach(x => x.NotifyProgress(BytesDownloaded));
    }

    public Task WaitForProgressAsync(long progress, int? timeout = null)
    {
      if (BytesTotal.HasValue == false)
        throw new InvalidOperationException($"Call {nameof(StartDownload)} first");

      var source = new ProgressCompletionSource(Math.Min(progress, BytesTotal ?? long.MaxValue), timeout);
      _progressStates.Add(source);
      return source.Task;
    }

    public void StartDownload(IStoredTrack track, int buffeSize = 16892)
    {
      if (State != State.Started)
        throw new InvalidOperationException($"Call {nameof(InitializeAsync)} first");

      Target = track;
      _targetStream = track.OpenWrite();
      _downloadBuffer = new byte[buffeSize];
      CurrentReadTask = _sourceStream.ReadAsync(_downloadBuffer, 0, _downloadBuffer.Length, _cancellationToken.Token);
      State = State.Downloading;
    }

    public async Task<bool> InitializeAsync()
    {
      if (State != State.Initialized)
        throw new InvalidOperationException("Can't initialize a download state again");

      var (url, length) = await Source.GetUrlAsync();
      if (url == null)
        return false;

      try
      {
        _httpClient = new HttpClient();
        _sourceStream = await _httpClient.GetStreamAsync(url);
      }
      catch (Exception ex)
      {
        Exception = ex;
        State = State.Errored;
        Dispose();
        return false;
      }

      BytesTotal = length;
      State = State.Started;
      return true;
    }

    public void Cancel()
    {
      _cancellationToken.Cancel();
      Dispose();
    }

    public void Dispose()
    {
      _progressStates.ForEach(x => x.Cancel());
      _progressStates.Clear();
      _targetStream?.Flush();
      _targetStream?.Dispose();
      _targetStream = null;
      _sourceStream?.Dispose();
      _sourceStream = null;
      _httpClient?.Dispose();
      _cancellationToken.Dispose();
    }
  }

  public enum State
  {
    Initialized = 0,
    Started = 1,
    Downloading = 2,
    Downloaded = 3,
    Errored = 4,
    Cancelled = 5
  }
}
