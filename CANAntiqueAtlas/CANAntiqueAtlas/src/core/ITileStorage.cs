using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.util;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CANAntiqueAtlas.src.core
{
    public interface ITileStorage
    {
        void SetTile(int x, int y, Tile tile);
        /** Returns the Tile previously set at given coordinates. */
        Tile RemoveTile(int x, int y);
        Tile GetTile(int x, int y);
        bool HasTileAt(int x, int y);
        Rect GetScope();
    }
}
