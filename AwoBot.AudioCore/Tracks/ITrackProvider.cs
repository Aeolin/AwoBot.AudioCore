using AwosFramework.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Tracks
{
  public interface ITrackProvider : IMachine<IAsyncEnumerable<ITrack>, string>
  {
    public string Id { get; }
    public Task<ITrack> TrySearchAsync(string query);
  }
}
