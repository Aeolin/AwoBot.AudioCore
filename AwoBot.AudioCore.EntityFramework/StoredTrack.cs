using AwoBot.AudioCore.Persistence;
using AwoBot.AudioCore.Tracks;
using System;
using System.IO;

namespace AwoBot.AudioCore.EntityFramework
{
  public class StoredTrack : IStoredTrack
  {
    public StoredTrack()
    {

    }

    public StoredTrack(ITrack track, string storagePath)
    {
      this.TrackId = track.Id;
      this.SourceId = track.Source.Id;
      var parentFolder = Path.Combine(storagePath, ReplaceInvalidChars(track.Source.Id));
      Directory.CreateDirectory(parentFolder);
      this.FilePath = Path.Combine(parentFolder, $"{ReplaceInvalidChars(track.Id)}.{track.AudioContainerType}");
      this.RequiresDownload = true;
    }

    private static string ReplaceInvalidChars(string filename)
    {
      return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
    }

    public string SourceId { get; set; }

    public string TrackId { get; set; }

    public string FilePath { get; set; }

    public bool RequiresDownload { get; set; }

    public Stream OpenRead()
    {
      return File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
    }

    public Stream OpenWrite()
    {
      return File.Open(FilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
    }
  }
}
