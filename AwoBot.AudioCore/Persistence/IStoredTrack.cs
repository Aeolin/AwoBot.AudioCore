using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Persistence
{
  public interface IStoredTrack
  {
    public string SourceId { get; init; }
    public string TrackId { get; init; }
    public Stream OpenRead();
    public Stream OpenWrite();
  }
}
