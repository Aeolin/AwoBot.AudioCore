using AwoBot.AudioCore.Playlists;
using AwoBot.AudioCore.Tracks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.TestBot
{
  public class BasicPlaylist : IPlaylist
  {
    private List<ITrack> _original = new List<ITrack>();
    private List<ITrack> _queue = new List<ITrack>();

    private bool _shuffeled = false;

    public ITrack this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public bool Shuffle { get => _shuffeled; set => setShuffle(value); }
    public LoopMode LoopMode { get; set; }

    private void setShuffle(bool value)
    {
      if (_shuffeled != value)
      {
        _shuffeled = value;
        _queue = new List<ITrack>(value ? _original.OrderBy(x => Guid.NewGuid()) : _original);
      }
    }

    public int Index { get; private set; }

    public ITrack NextTrack => _queue.Skip(Index).FirstOrDefault();
    public ITrack CurrentTrack => _queue.Skip(Index - 1).FirstOrDefault();

    public int Count => _queue.Count;

    public bool IsReadOnly => false;

    public event Action<ITrack> OnTrackAdded;
    public event Action<ITrack> OnTrackRemoved;
    public event Action<LoopMode> OnLoopModeChanged;
    public event Action<bool> OnShuffleToggeled;

    public void Add(ITrack item)
    {
      _original.Add(item);
      _queue.Add(item);
      OnTrackAdded?.Invoke(item);
    }

    public void Clear()
    {
      _original.Clear();
      _queue.Clear();
    }

    public bool Contains(ITrack item)
    {
      return _original.Contains(item);
    }

    public void CopyTo(ITrack[] array, int arrayIndex)
    {
      _queue.CopyTo(array, arrayIndex);
    }

    public IEnumerator<ITrack> GetEnumerator()
    {
      return _queue.GetEnumerator();
    }

    public int IndexOf(ITrack item)
    {
      return _queue.IndexOf(item);
    }

    public void Insert(int index, ITrack item)
    {
      _original.Insert(index, item);
      _queue.Insert(index, item);
      OnTrackAdded?.Invoke(item);
    }

    public ITrack Next()
    {
      if (LoopMode == LoopMode.Repeat)
        return CurrentTrack;

      if (Index < _original.Count)
      {
        Index++;
        return CurrentTrack;
      }
      else if (LoopMode == LoopMode.Loop)
      {
        Index = 0;
        return CurrentTrack;
      }

      return null;
    }

    public ITrack Previous()
    {
      if (LoopMode == LoopMode.Repeat)
        return CurrentTrack;

      if (Index > 0)
      {
        Index--;
        return CurrentTrack;
      }
      else if (LoopMode == LoopMode.Loop)
      {
        Index = _queue.Count - 1;
        return CurrentTrack;
      }

      return null;
    }

    public bool Remove(ITrack item)
    {
      _queue.Remove(item);
      if (_original.Remove(item))
      {
        OnTrackRemoved?.Invoke(item);
        return true;
      }

      return false;
    }

    public void RemoveAt(int index)
    {
      var track = _queue[index];
      _queue.RemoveAt(index);
      _original.Remove(track);
      OnTrackRemoved?.Invoke(track);
    }

    public void SetIndex(int index)
    {
      if(index >= 0 && index < _original.Count)
      Index = index;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return _queue.GetEnumerator();
    }
  }
}
