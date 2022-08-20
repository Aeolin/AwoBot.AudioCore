using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Persistence.StoredTracks
{
  public class LocalStoredTrack : IStoredTrack
  {
    public string SourceId { get; init; }
    public string TrackId { get; init; }
    public string FilePath { get; init; }

    public LocalStoredTrack()
    {

    }

    public LocalStoredTrack(string sourceId, string trackId, string filePath)
    {
      SourceId=sourceId;
      TrackId=trackId;
      FilePath=filePath;
    }


    public Stream OpenRead()
    {
      return File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
    }

    public Stream OpenWrite()
    {
      return File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
    }
  }
}
