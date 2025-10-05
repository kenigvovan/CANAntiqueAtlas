using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.util;

namespace CANAntiqueAtlas.src.core
{
    public interface ISeenTileStorage
    {
        void SetTile(int x, int y, bool Tile);
        /** Returns the Tile previously set at given coordinates. */
        bool RemoveTile(int x, int y);
        bool GetTile(int x, int y);
        bool HasTileAt(int x, int y);
    }
}
