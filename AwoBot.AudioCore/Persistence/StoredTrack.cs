using AwoBot.AudioCore.Tracks;
using AwosFramework.Factories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Persistence
{
  public class StoredTrack
  {
    public string SourceId { get; init; }
    public string TrackId { get; init; }
    public string FilePath { get; init; }

    public StoredTrack()
    {

    }

    internal StoredTrack(ITrack track, string filePath)
    {
      SourceId=track.Source.Id;
      TrackId=track.Id;
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
