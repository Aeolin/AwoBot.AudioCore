using AwoBot.AudioCore.Tracks;
using AwosFramework.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode;

namespace AwoBot.AudioCore.Youtube
{
  public class YoutubeTrackMachine : ITrackProvider
  {
    private YoutubeClient _client;
    public YoutubeTrackMachine(YoutubeClient client)
    {
      _client = client;
    }

    public string Id => "YouTube";

    private Dictionary<string, string> youtubeQueryParams(string url)
    {
      if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
      {
        if (uri.Host != "www.youtube.com" || uri.Host != "www.youtu.be")
          return null;

        if (uri.Segments.Length < 1)
          return null;

        if ((uri.Segments[0].Equals("playlist") || uri.Segments[0].Equals("watch")) == false)
          return null;

        var queryParameters = uri.Query.Substring(1).Split("&").Select(x => x.Split("=")).ToDictionary(x => x[0], x => x[1]);
        return queryParameters;
      }
      return null;
    }

    public bool CanConstruct(string parameter)
    {
      var query = youtubeQueryParams(parameter);
      if (query == null)
        return false;

      return query.ContainsKey("v") || query.ContainsKey("list");
    }

    public async IAsyncEnumerable<ITrack> Construct(string url)
    {
      var query = youtubeQueryParams(url);
      if (query == null)
        throw new ArgumentException($"the given url is not a valid youtube video or playlist", nameof(url));
 
      if(query.TryGetValue("list", out var listId))
      {
        var list = await _client.Playlists.GetVideosAsync(listId).ToListAsync();
        foreach (var video in list)
          yield return new YoutubeTrack(video, _client, this);
      }
      else if(query.TryGetValue("v", out var videoId))
      {
        var video = await _client.Videos.GetAsync(videoId);
        yield return new YoutubeTrack(video, _client, this);
      }
      else
      {
        yield break;
      }
    }

    public async Task<ITrack> TrySearchAsync(string query)
    {
      var result = await _client.Search.GetVideosAsync(query).ToListAsync();
      if (result.Count == 0)
        return null;

      return new YoutubeTrack(result.First(), _client, this); 
    }
  }
}
