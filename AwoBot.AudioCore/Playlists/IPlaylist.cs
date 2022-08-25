using AwoBot.AudioCore.Tracks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Playlists
{
  public interface IPlaylist : IList<ITrack>
  {
    public event Action<ITrack> OnTrackAdded;
    public event Action<ITrack> OnTrackRemoved;
    public event Action<LoopMode> OnLoopModeChanged;
    public event Action<bool> OnShuffleToggeled;

    public bool Shuffle { get; set; }
    public int Index { get; }
    public void SetIndex(int index);
    public LoopMode LoopMode { get; set; } 
    public ITrack NextTrack { get; }
    public ITrack CurrentTrack { get; }
    public ITrack Next();
    public ITrack Previous();
  }

}
