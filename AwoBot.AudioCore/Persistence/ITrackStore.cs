using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Persistence
{
  public interface ITrackStore
  {
    public Task<StoredTrack> FindTrackAsync(string id, string sourceId);
    public Task<IEnumerable<StoredTrack>> GetStoredTracksAsync();
    public Task AddTrackAsync(StoredTrack track);
    public Task RemoveTrackAsync(StoredTrack track);
  }
}
