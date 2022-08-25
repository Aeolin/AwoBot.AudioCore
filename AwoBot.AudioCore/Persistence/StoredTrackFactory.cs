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
    private ITrackStorage _storage;

    public StoredTrackFactory(ITrackStorage storage)
    {
      _storage = storage;
    }


    public async Task<IStoredTrack> GetStoredTrackAsync(ITrack track)
    {
      var stored = await _storage.FindTrackAsync(track.Id, track.Source.Id);
      if (stored == null)
        stored = await _storage.GetOrCreateTrackAsync(track);

      return stored;
    }
  }
}
