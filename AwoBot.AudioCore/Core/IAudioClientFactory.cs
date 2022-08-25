using Discord;
using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Core
{
  public interface IAudioClientFactory
  {
    public Task<IAudioClient> GetAudioClientAsync(IVoiceChannel channel);
  }
}
