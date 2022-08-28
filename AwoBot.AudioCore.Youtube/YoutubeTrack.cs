using AwoBot.AudioCore.Tracks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace AwoBot.AudioCore.Youtube
{
  public class YoutubeTrack : ITrack
  {
    private readonly YoutubeClient _client;
    private readonly IVideo _video;

    public YoutubeTrack(IVideo video, YoutubeClient client, ITrackProvider parent)
    {
      this._client = client;
      this._video = video;
      this.Source = parent;
      this.Id = video.Id;
      this.Name = video.Title;
      this.ThumbnailUrl = video.Thumbnails.OrderByDescending(x => x.Resolution.Area).FirstOrDefault()?.Url;
      this.Author = video.Author.ChannelTitle;
      this.Duration = video.Duration;
      this.AudioContainerType = "trck";
    }

    public string Name { get; init; }
    public string Author { get; init; }
    public string ThumbnailUrl { get; init; }
    public TimeSpan? Duration { get; init; }
    public ITrackProvider Source { get; init; }
    public string Id { get; init; }
    public string AudioContainerType { get; private set; }

    public override string ToString()
    {
      return $"{Name} - {Author}";
    }

    public async Task<(Uri, long?)> GetUrlAsync()
    {
      var manifest = await _client.Videos.Streams.GetManifestAsync(_video.Id);
      var info = manifest.GetAudioStreams().OrderByDescending(x => x.Bitrate).ThenByDescending(x => x is AudioOnlyStreamInfo).First();
      AudioContainerType = info.Container.Name;
      return (new Uri(info.Url), info.Size.Bytes);
    }
  }
}
