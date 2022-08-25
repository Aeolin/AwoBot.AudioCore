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
    public string SourceId { get; private set; }
    public string TrackId { get; private set; }
    public string FilePath { get; private set; }
    public bool RequiresDownload { get; private set; }

    public StoredTrack()
    {

    }

    internal StoredTrack(ITrack track, string filePath)
    {
      SourceId = track.Source.Id;
      TrackId = track.Id;
      FilePath = filePath;
    }

    public Stream OpenRead()
    {
      return File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
    }

    public Stream OpenWrite()
    {
      RequiresDownload = true;
      return File.Open(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
    }
  }
}
