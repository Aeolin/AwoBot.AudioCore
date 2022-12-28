using Discord;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Core
{
  public interface IAudioPlayerFactory
  {
    Task<IAudioPlayer> GetOrCreateAudioPlayerAsync(IGuildUser user);
    bool TryGetExistingAudioPlayer(IGuildUser user, out IAudioPlayer player);
  }
}