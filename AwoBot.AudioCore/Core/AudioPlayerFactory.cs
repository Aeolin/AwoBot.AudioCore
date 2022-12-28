using AwoBot.AudioCore.Playlists;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Core
{
  public class AudioPlayerFactory : IAudioPlayerFactory
  {
    private readonly Dictionary<IVoiceChannel, AudioPlayer> _audioPlayers = new Dictionary<IVoiceChannel, AudioPlayer>();
    private readonly IServiceProvider _serviceProvider;
    private readonly IPlaylistFactory _playlistFactory;


    public AudioPlayerFactory(IServiceProvider serviceProvider, IPlaylistFactory playlistFactory)
    {
      _serviceProvider = serviceProvider;
      _playlistFactory=playlistFactory;
    }

    public async Task<IAudioPlayer> GetOrCreateAudioPlayerAsync(IGuildUser user)
    {
      if (user.VoiceChannel == null)
        return null;

      if (_audioPlayers.TryGetValue(user.VoiceChannel, out var audioPlayer) == false)
      {
        audioPlayer = _serviceProvider.GetService<AudioPlayer>();
        audioPlayer.SetVoiceChannel(user.VoiceChannel);
        await audioPlayer.SetPlaylistAsync(_playlistFactory.CreatePlaylist());
        var result = await audioPlayer.PlayAsync();
        if (result)
        {
          _audioPlayers[user.VoiceChannel] = audioPlayer;
        }
        else
        {
          audioPlayer = null;
        }
      }

      return audioPlayer;
    }

    public bool TryGetExistingAudioPlayer(IGuildUser user, out IAudioPlayer player)
    {
      var res = _audioPlayers.TryGetValue(user.VoiceChannel, out var audioPlayer);
      player = audioPlayer;
      return res;
    }
  }
}
