using AwoBot.AudioCore.Persistence;
using AwoBot.AudioCore.Tracks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.EntityFramework
{
  public class StoredTrackContext : DbContext, ITrackStorage
  {
    private StoredTrackConfig _config;
    public DbSet<StoredTrack> StoredTracks { get; set; }

    public StoredTrackContext(StoredTrackConfig config)
    {
      _config = config;
    }


    public async Task<IStoredTrack> FindTrackAsync(string id, string sourceId)
    {
      var value = await StoredTracks.FindAsync(id, sourceId);
      return value;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseSqlServer();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<StoredTrack>().HasKey(x => new { x.TrackId, x.SourceId });
      base.OnModelCreating(modelBuilder);
    }

    public async Task<IStoredTrack> GetOrCreateTrackAsync(ITrack track)
    {
      var value = await StoredTracks.FindAsync(track.Id, track.Source.Id);
      if (value == null)
      {
        value = new StoredTrack(track, _config.LocalPath);
        await StoredTracks.AddAsync(value);
        await SaveChangesAsync();
      }

      return value;
    }

    public async Task<IEnumerable<IStoredTrack>> GetStoredTracksAsync()
    {
      var list = await StoredTracks.AsAsyncEnumerable()
        .Cast<IStoredTrack>()
        .ToListAsync()
        .AsTask();

      return list;
    }

    public async Task MarkDownloadedAsync(IStoredTrack track)
    {
      var value = await StoredTracks.FindAsync(track.TrackId, track.SourceId);
      if (value != null)
      {
        value.RequiresDownload = false;
        await SaveChangesAsync();
      }
    }

    public async Task RemoveTrackAsync(IStoredTrack track)
    {
      var value = await StoredTracks.FindAsync(track.TrackId, track.SourceId);
      if (value != null)
      {
        StoredTracks.Remove(value);
        await SaveChangesAsync();
      }
    }
  }
}
