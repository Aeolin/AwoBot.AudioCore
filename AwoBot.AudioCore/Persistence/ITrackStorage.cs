using AwoBot.AudioCore.Tracks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Persistence
{
  public interface ITrackStorage
  {
    public Task<IStoredTrack> FindTrackAsync(string id, string sourceId);
    public Task<IEnumerable<IStoredTrack>> GetStoredTracksAsync();
    public Task RemoveTrackAsync(IStoredTrack track);
    public Task<IStoredTrack> GetOrCreateTrackAsync(ITrack track);
    public Task MarkDownloadedAsync(IStoredTrack track);
  }
}
