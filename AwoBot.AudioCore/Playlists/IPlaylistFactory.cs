using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Playlists
{
  public interface IPlaylistFactory
  {
    public IPlaylist CreatePlaylist();
  }
}
