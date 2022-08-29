using AwoBot.AudioCore.Playlists;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.TestBot
{
  public class BasicPlaylistFactory : IPlaylistFactory
  {
    public IPlaylist CreatePlaylist()
    {
      return new BasicPlaylist();
    }
  }
}
