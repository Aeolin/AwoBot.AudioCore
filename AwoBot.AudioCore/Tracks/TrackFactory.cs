using AwosFramework.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Tracks
{
  public class TrackFactory : AbstractFactoryBase<IAsyncEnumerable<ITrack>, string, ITrackProvider>, ITrackFactory
  {
    public TrackFactory(bool autoDiscover = true, IServiceProvider provider = null) : base(autoDiscover, provider)
    {

    }

    public async Task<IEnumerable<ITrack>> SearchOrGetTracksAsync(string urlOrQuery)
    {
      if (base.TryConstruct(urlOrQuery, out var items))
        return await items.ToArrayAsync();

      var results = await Task.WhenAll(this.Select(x => x.TrySearchAsync(urlOrQuery)));
      var result = results.Where(x => x != null).FirstOrDefault();
      if (result != null)
        return new[] { result };

      return null;
    }
  }
}
