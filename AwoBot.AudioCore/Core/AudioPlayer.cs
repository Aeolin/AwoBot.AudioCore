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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Core
{
  public class AudioPlayer
  {
    private ILogger _logger;
    private IPlaylist _playlist;
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
    public bool Paused { get; private set; }

    public AudioPlayer(FFOptions _ffOptions, ILoggerFactory factory, IPlaylist playlist, DownloadManager downloadManager, IAudioClientFactory audioFactory, IVoiceChannel voiceChannel, DiscordSocketClient client)
    {
      _ffmpegOptions = _ffOptions;
      _logger = factory.CreateLogger<AudioPlayer>();
      _playlist = playlist;
      _downloadManager = downloadManager;
      _audioFactory = audioFactory;
      _voiceChannel = voiceChannel;
      _client = client;
      _cancelCurrentTrack = new CancellationTokenSource();
      _songAddedTrigger = new ManualResetEvent(false);
      _playlist.OnTrackAdded += _playlist_OnTrackAdded;
    }

    private async void _playlist_OnTrackAdded(ITrack track)
    {
      if (_playlist.CurrentTrack == null ||_playlist.NextTrack == null)
        await _downloadManager.QueueForDownloadAsync(track);
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

    public async Task<bool> PlayAsync()
    {
      if (Paused)
      {
        if (await ensureAudioClientCreatedAsync() == false)
          return false;

        Paused = false;
        _unpauseTrigger.Set();
      }

      return true;
    }

    public void Pause()
    {
      if (Paused == false)
      {
        Paused = true;
        _unpauseTrigger = new ManualResetEvent(false);
        _cancelCurrentTrack.Cancel();
      }
    }

    public void Stop()
    {
      if (_playerThread != null)
      {
        _running = false;
        _cancelCurrentTrack.Cancel();
        _songAddedTrigger.Set();
        if (_unpauseTrigger != null)
          _unpauseTrigger.Set();

        if (_playerThread.Join(5000) == false)
        {
          _logger.LogError("Can't stop player thread");
          if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            _playerThread.Abort();
          else
            _logger.LogCritical("Fuck");
        }

      }
    }

    private void onProgress(TimeSpan progress)
    {
      Progress = progress;
    }

    private async void playerThreadWorker()
    {
      await ensureAudioClientCreatedAsync();
      while (_running)
      {
        if (_unpauseTrigger != null)
        {
          await _unpauseTrigger.WaitOneAsync();
          _unpauseTrigger.Dispose();
          _unpauseTrigger = null;
        }

        if (_playlist.CurrentTrack == null)
        {
          await _songAddedTrigger.WaitOneAsync(20000);
          continue;
        }

        var stream = await _downloadManager.OpenStreamAsync(_playlist.CurrentTrack);
        if (stream == null)
        {
          await Task.Delay(5000);
          _playlist.Next();
          _logger.LogError($"Couldn't open stream for track {_playlist.CurrentTrack}, skipping it...");
          continue;
        }

        var ffmpeg = FFMpegArguments
          .FromPipeInput(new StreamPipeSource(stream), options =>
          {
            options.Seek(Progress);
          })
          .OutputToPipe(new StreamPipeSink(_discordPCMStream), options =>
          {
            options.WithAudioSamplingRate(48000);
            options.ForceFormat("s16le");
            options.WithCustomArgument("-ac 2 -af \"volume=0.4\"");
          })
          .CancellableThrough(_cancelCurrentTrack.Token)
          .NotifyOnProgress(onProgress);

        try
        {
          await ffmpeg.ProcessAsynchronously(true, _ffmpegOptions);
        }
        catch (TaskCanceledException)
        {
          _logger.LogDebug($"Player was cancelled (expected)");
          continue;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, $"Error playing track {_playlist.CurrentTrack}, skipping to next...");
        }
        finally
        {
          if (Paused == false)
          {
            Progress = null; // save progress only when paused
            _playlist.Next();
          }

          stream.Dispose();
          stream = null;
        }

      }
    }
  }
}
