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
    public string SourceId { get; }
    public string TrackId { get; }
    public bool RequiresDownload { get; }

    public Stream OpenRead();
    public Stream OpenWrite();
  }
}
