using AwoBot;
using AwoBot.AudioCore.Core;
using AwoBot.AudioCore.Download;
using AwoBot.AudioCore.EntityFramework;
using AwoBot.AudioCore.Persistence;
using AwoBot.AudioCore.Playlists;
using AwoBot.AudioCore.TestBot;
using AwoBot.AudioCore.Tracks;
using AwoBot.AudioCore.Youtube;
using AwoBot.Logging.Discord;
using AwosFramework.Utils;
using Discord;
using Discord.WebSocket;
using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReInject;
using YoutubeExplode;

await BotRunner.Run(bot =>
{
  var type = typeof(YoutubeTrack);
  var config = bot.Container.GetInstance<IConfiguration>();
  bot.Container.Register<StoredTrackConfig>(DependencyStrategy.AtomicInstance, true, new StoredTrackConfig { LocalPath = "./tracks" })
    .Register<YoutubeClient>(DependencyStrategy.AtomicInstance, true, new YoutubeClient())
    .Register<IPlaylistFactory>(DependencyStrategy.AtomicInstance, true, new BasicPlaylistFactory())
    .Register<ITrackFactory>(DependencyStrategy.AtomicInstance, true, new TrackFactory(true, bot.Container))
    .Register<DbContextOptions<StoredTrackContext>>(DependencyStrategy.AtomicInstance, true, new DbContextOptionsBuilder<StoredTrackContext>().UseSqlServer(config["access:database"]).Options)
    .Register<ITrackStorage, StoredTrackContext>(DependencyStrategy.NewInstance)
    .Register<StoredTrackFactory>(DependencyStrategy.NewInstance)
    .Register<DownloadManager>(DependencyStrategy.AtomicInstance, true, bot.Container.GetInstance<DownloadManager>())
    .Register<IAudioClientFactory, AudioClientFactory>(DependencyStrategy.CachedInstance)
    .Register<AudioPlayer>(DependencyStrategy.NewInstance)
    .Register<IServiceProvider>(DependencyStrategy.AtomicInstance, true, bot.Container)
    .Register<IAudioPlayerFactory>(DependencyStrategy.AtomicInstance, true, bot.Container.GetInstance<AudioPlayerFactory>())
    .Register<FFOptions>(DependencyStrategy.AtomicInstance, true, new FFOptions
    {
      BinaryFolder = $"./ffmpeg/{Environment.OSVersion.Platform.ToString().ToLower()}/",
      TemporaryFilesFolder = "./tmp"
    });
  bot.OnStateChanged +=Bot_OnStateChanged;
}, dconfig: new DiscordSocketConfig()
{
  AlwaysDownloadUsers = true,
  LogLevel = LogSeverity.Verbose,
  AlwaysDownloadDefaultStickers = true,
  GatewayIntents = GatewayIntents.All,
  MessageCacheSize = 100,
  LargeThreshold = 250
});

async void Bot_OnStateChanged(IBot bot, BotState state)
{
  switch (state)
  {
    case BotState.Connected:
      var container = bot.Container;
      var channelId = container.GetInstance<IConfiguration>().GetValue<ulong?>("logging:discord-channel-id", null);
      if (channelId.HasValue)
      {
        var channel = await container.GetInstance<DiscordSocketClient>().GetChannelAsync(channelId.Value) as ITextChannel;
        if (channel != null)
        {
          bot.Container.GetInstance<ILoggerFactory>().AddDiscordChannelLogger(cfg =>
          {
            cfg.Channel = channel;
            cfg.LogLevels.AddRange(Enum.GetValues<LogLevel>());
            cfg.LoggingPeriod = TimeSpan.FromSeconds(10);
            cfg.FileUploadPeriod = TimeSpan.FromDays(1);
            cfg.FirstUploadDate = DateTime.UtcNow.Date.AddDays(1);
          });
        }
      }
      break;
  }
}