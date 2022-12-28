using AwoBot.AudioCore.Download;
using AwoBot.AudioCore.Playlists;
using AwoBot.AudioCore.Tracks;
using AwoBot.AudioCore.Utils;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Core
{
  public class AudioPlayer : IAudioPlayer
  {
    private static readonly Regex ProgressRegex = new Regex(@"time=(\d\d:\d\d:\d\d.\d\d?)", RegexOptions.Compiled);
    public IPlaylist Playlist { get; private set; }
    private DownloadManager _downloadManager;
    private IAudioClientFactory _audioFactory;
    private IAudioClient _audioClient;
    private IVoiceChannel _voiceChannel;
    private DiscordSocketClient _client;
    private volatile bool _running;
    private CancellationTokenSource _cancelCurrentTrack;
    private ManualResetEvent _songAddedTrigger;
    private ManualResetEvent _unpauseTrigger = null;
    private TimeSpan? Progress;
    private Stream _discordPCMStream;
    private FFOptions _ffmpegOptions;
    private Thread _playerThread;

    // Semaphores for thread safety
    private SemaphoreSlim _playingSemaphore = new SemaphoreSlim(1);
    private SemaphoreSlim _pausingSemaphore = new SemaphoreSlim(1);
    private SemaphoreSlim _stoppingSemaphore = new SemaphoreSlim(1);
    private ILogger _logger;
    public AudioPlayerState State { get; private set; }

    public AudioPlayer(FFOptions _ffOptions, ILoggerFactory factory, DownloadManager downloadManager, IAudioClientFactory audioFactory, DiscordSocketClient client)
    {
      _ffmpegOptions = _ffOptions;
      _logger = factory.CreateLogger<AudioPlayer>();
      _downloadManager = downloadManager;
      _audioFactory = audioFactory;
      _client = client;
      _songAddedTrigger = new ManualResetEvent(false);
    }

    private async void _playlist_OnTrackAdded(ITrack track)
    {
      if (Playlist.CurrentTrack == null || Playlist.NextTrack == null)
        await _downloadManager.QueueForDownloadAsync(track);

      _songAddedTrigger.Set();
    }

    public async Task SetPlaylistAsync(IPlaylist playlist)
    {
      if (playlist == null)
        throw new ArgumentNullException(nameof(playlist));

      if (Playlist != null)
        Playlist.OnTrackAdded -= _playlist_OnTrackAdded;

      Playlist = playlist;
      Playlist.OnTrackAdded += _playlist_OnTrackAdded;
      if (Playlist.CurrentTrack != null)
        await _downloadManager.QueueForDownloadAsync(Playlist.CurrentTrack);

      if (Playlist.NextTrack != null)
        await _downloadManager.QueueForDownloadAsync(Playlist.NextTrack);

      if (Playlist.Count > 0)
        _songAddedTrigger.Set();

    }

    private async Task<bool> ensureAudioClientCreatedAsync()
    {
      if (_audioClient == null)
      {
        _audioClient = await _audioFactory.GetAudioClientAsync(_voiceChannel);
        if (_audioClient == null)
        {
          _logger.LogError($"Error creating audioclient for channel {_voiceChannel.Name} in guild {_voiceChannel.Guild.Name}");
          return false;
        }

        _discordPCMStream?.Dispose();
        _discordPCMStream = _audioClient.CreatePCMStream(AudioApplication.Music);
      }

      return true;
    }

    public void SetVoiceChannel(IVoiceChannel channel)
    {
      _voiceChannel = channel;
    }

    public async Task<bool> PlayAsync()
    {
      try
      {
        _running = true;
        _playingSemaphore.Wait();
        if (State != AudioPlayerState.Playing)
        {
          if (await ensureAudioClientCreatedAsync() == false)
            return false;

          if (_playerThread == null)
          {
            _playerThread = new Thread(playerThreadWorker);
            _playerThread.Start();
          }

          State = AudioPlayerState.Playing;
          _unpauseTrigger?.Set();
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error starting player");
        return false;
      }
      finally
      {
        _playingSemaphore.Release();
      }

      return true;
    }

    public void Pause()
    {
      _pausingSemaphore.Wait();
      if (State == AudioPlayerState.Playing)
      {
        _unpauseTrigger = new ManualResetEvent(false);
        State = AudioPlayerState.Paused;
        _cancelCurrentTrack?.Cancel();
      }
      _pausingSemaphore.Release();
    }

    public void Stop()
    {
      try
      {
        _stoppingSemaphore.Wait();
        if (_playerThread != null && State != AudioPlayerState.Stopped)
        {
          _running = false;
          _cancelCurrentTrack?.Cancel();
          _songAddedTrigger.Set();
          if (_unpauseTrigger != null)
            _unpauseTrigger.Set();

          if (_playerThread.Join(5000) == false)
            _logger.LogError("Can't stop player thread");


          _audioClient.Dispose();
          _discordPCMStream.Dispose();
          _audioClient = null;
          _discordPCMStream = null;
          _playerThread = null;
          State = AudioPlayerState.Stopped;
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error stopping player");
      }
      finally
      {
        _stoppingSemaphore.Release();
      }
    }

    private void onProgress(TimeSpan progress)
    {
      Progress = progress;
    }

    private async void playerThreadWorker()
    {
      if (await ensureAudioClientCreatedAsync() == false)
        return;

      while (_running)
      {
        if (_unpauseTrigger != null)
        {
          _unpauseTrigger.WaitOne();
          _unpauseTrigger.Dispose();
          _unpauseTrigger = null;
        }

        if (Playlist.CurrentTrack == null)
        {
          _songAddedTrigger.WaitOne(20000);
          continue;
        }

        var stream = await _downloadManager.OpenStreamAsync(Playlist.CurrentTrack);
        if (stream == null)
        {
          await Task.Delay(5000);
          Playlist.Next();
          _logger.LogError($"Couldn't open stream for track {Playlist.CurrentTrack}, skipping it...");
          continue;
        }

        _cancelCurrentTrack = new CancellationTokenSource();
        var ffmpeg = FFMpegArguments
          .FromPipeInput(new StreamPipeSource(stream), options =>
          {
            options.Seek(Progress);
          })
          .OutputToPipe(new StreamPipeSink(_discordPCMStream), options =>
          {
            options.WithAudioSamplingRate(48000);
            options.ForceFormat("s16le");
          })
          // hacky way to fix an issue in ffmpeg.core since the progress data is sent to stderror and not stdout
          // see https://github.com/rosenbjerg/FFMpegCore/issues/331
          .NotifyOnError(x => { 
            var match = ProgressRegex.Match(x);
            if (!match.Success) 
              return;

            var processed = TimeSpan.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            onProgress(processed);
          })
          .CancellableThrough(_cancelCurrentTrack.Token);

        try
        {
          await ffmpeg.ProcessAsynchronously(true, _ffmpegOptions);
        }
        catch (OperationCanceledException)
        {
          _logger.LogDebug($"Player was cancelled (expected)");
          continue;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, $"Error playing track {Playlist.CurrentTrack}, skipping to next...");
        }
        finally
        {
          if (State == AudioPlayerState.Playing)
          {
            Progress = null;
            Playlist.Next();
            if (Playlist.NextTrack != null)
              await _downloadManager.QueueForDownloadAsync(Playlist.NextTrack);
          }

          stream.Dispose();
          stream = null;
        }
      }
    }
  }
}
