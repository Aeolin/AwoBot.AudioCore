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
    private TrackFactory _trackFactory;

    public AudioCommand(IDependencyContainer dependencyContainer, TrackFactory trackFactory)
    {
      _dependencyContainer=dependencyContainer;
      _trackFactory=trackFactory;
    }

    [Command("play")]
    [Alias("p")]
    [RequireContext(ContextType.Guild)]
    public async Task Play(string url)
    {
      var user = Context.User as IGuildUser;
      if(user.VoiceChannel != null)
      {
        var tracks = await _trackFactory.SearchOrGetTracksAsync(url);
        if(tracks.Count() > 0)
        {
          var playlist = new BasicPlaylist();
          foreach (var track in tracks)
            playlist.Add(track);

          var player = _dependencyContainer.GetInstance<AudioPlayer>();
          player.SetVoiceChannel(user.VoiceChannel);
          await player.SetPlaylistAsync(playlist);
          await player.PlayAsync();
        }
      }
    }
  }
}
