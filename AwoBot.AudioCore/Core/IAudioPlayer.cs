using AwoBot.AudioCore.Playlists;
using Discord;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Core
{
  public interface IAudioPlayer
  {
    AudioPlayerState State { get; }
    public IPlaylist Playlist { get; }
    void Pause();
    Task<bool> PlayAsync();
    void Stop();
  }
}