using Discord;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Core
{
  public interface IAudioPlayerFactory
  {
    Task<IAudioPlayer> GetAudioPlayerAsync(IGuildUser user);
  }
}