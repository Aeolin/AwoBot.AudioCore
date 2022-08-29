using System.Collections.Generic;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Tracks
{
  public interface ITrackFactory
  {
    Task<IEnumerable<ITrack>> SearchOrGetTracksAsync(string urlOrQuery);
  }
}