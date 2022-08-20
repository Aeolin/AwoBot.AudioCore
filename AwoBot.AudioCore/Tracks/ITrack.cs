using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Tracks
{
  public interface ITrack
  {
    public string Name { get; }
    public string ThumbnailUrl { get; }
    public TimeSpan Duration { get; }
    public ITrackProvider Source { get; }
    public string SourceId { get; }
    public Task<bool> TryOpenStreamAsync(out Stream stream, out long length);
  
  }
}
