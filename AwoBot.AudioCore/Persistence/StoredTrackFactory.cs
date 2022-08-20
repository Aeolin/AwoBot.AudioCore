using AwoBot.AudioCore.Tracks;
using ReInject.Implementation.Attributes;
using ReInject.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Persistence
{
  public class StoredTrackFactory
  {
    private StoredTrackConfig _config;
    private ITrackStore _storage;

    public StoredTrackFactory(StoredTrackConfig config, ITrackStore storage)
    {
      _config=config;
      _storage=storage;
    }

    public async Task<StoredTrack> GetStoredTrackAsync(ITrack track)
    {
      var stored = await _storage.FindTrackAsync(track.Id, track.Source.Id);
      if (stored == null)
      {
        stored = new StoredTrack(track, Path.Combine(_config.LocalPath, track.Source.Id, $"{track.Id}.{track.AudioContainerType}"));
        await _storage.AddTrackAsync(stored);
      }

      return stored;
    }
  }
}
