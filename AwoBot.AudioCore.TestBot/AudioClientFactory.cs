using AwoBot.AudioCore.Core;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.TestBot
{
  public class AudioClientFactory : IAudioClientFactory
  {
    private DiscordSocketClient _client;

    public AudioClientFactory(DiscordSocketClient client)
    {
      _client=client;
    }

    public async Task<IAudioClient> GetAudioClientAsync(IVoiceChannel iChannel)
    {
      if (iChannel is SocketVoiceChannel voiceChannel == false) 
        voiceChannel = (await _client.GetChannelAsync(iChannel.Id)) as SocketVoiceChannel;

      return await voiceChannel.ConnectAsync(true, false, false);
    }
  }
}
