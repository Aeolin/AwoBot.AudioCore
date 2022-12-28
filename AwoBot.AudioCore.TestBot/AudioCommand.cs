using AwoBot.AudioCore.Core;
using AwoBot.AudioCore.Tracks;
using Discord;
using Discord.Commands;
using ReInject.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.TestBot
{
  public class AudioCommand : ModuleBase
  {
    private IDependencyContainer _dependencyContainer;
    private ITrackFactory _trackFactory;
    private IAudioPlayerFactory _audioPlayerFactory;

    public AudioCommand(IDependencyContainer dependencyContainer, ITrackFactory trackFactory, IAudioPlayerFactory audioPlayerFactory)
    {
      _dependencyContainer=dependencyContainer;
      _trackFactory=trackFactory;
      _audioPlayerFactory=audioPlayerFactory;
    }

    [Command("play", RunMode = RunMode.Async)]
    [Alias("p")]
    [RequireContext(ContextType.Guild)] 
    public async Task Play([Remainder]string url)
    {
      var user = Context.User as IGuildUser;
      if(user.VoiceChannel != null)
      {
        var tracks = await _trackFactory.SearchOrGetTracksAsync(url);
        if(tracks.Count() > 0)
        {
          var player = await _audioPlayerFactory.GetOrCreateAudioPlayerAsync(user);
          foreach (var track in tracks)
            player.Playlist.Add(track);
          await player.PlayAsync();
        }
      }
    }

    [RequireContext(ContextType.Guild)]
    [Command("pause", RunMode = RunMode.Async)]
    public async Task Pause()
    {
      if(_audioPlayerFactory.TryGetExistingAudioPlayer(Context.User as IGuildUser, out var player))
      {
        if(player.State != AudioPlayerState.Paused)
        {
          player.Pause();
          await ReplyAsync("Paused");
        }
        else
        {
          await ReplyAsync("Already Paused");
        }
      }
    }

    [RequireContext(ContextType.Guild)]
    [Command("resume", RunMode = RunMode.Async)]
    public async Task Resume()
    {
      if (_audioPlayerFactory.TryGetExistingAudioPlayer(Context.User as IGuildUser, out var player))
      {
        if (player.State != AudioPlayerState.Playing)
        {
          await player.PlayAsync();
          await ReplyAsync("Playing");
        }
        else
        {
          await ReplyAsync("Already playing");
        }
      }
    }
  }
}
