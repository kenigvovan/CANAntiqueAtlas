using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.util;
using ProtoBuf;

namespace CANAntiqueAtlas.src.core
{
    [ProtoContract]
    public class TileGroupSeen: ISeenTileStorage
    {
        public static string TAG_POSITION = "p";
        public static string TAG_TILES = "t";

        /** The width/height of this TileGroup */
        public static int CHUNK_STEP = 16;

        /** The area of chunks this group covers */
        [ProtoMember(2)]
        public Rect scope;// = new Rect(0, 0, CHUNK_STEP, CHUNK_STEP);

        /** The tiles in this scope */
        [ProtoMember(1)]
        TileSeen[] tiles;// = new TileSeen[CHUNK_STEP * CHUNK_STEP];
        public TileGroupSeen()
        {
           // tiles = new TileSeen[CHUNK_STEP * CHUNK_STEP];
            //scope = new Rect(0, 0, CHUNK_STEP, CHUNK_STEP);
            //tiles = new Tile[CHUNK_STEP * CHUNK_STEP];
        }
        public TileGroupSeen(int x, int y)
        {
            // tiles = new Tile[CHUNK_STEP * CHUNK_STEP];
            //scope = new Rect(0, 0, CHUNK_STEP, CHUNK_STEP);
            tiles = new TileSeen[CHUNK_STEP * CHUNK_STEP];
            scope = new Rect(0, 0, CHUNK_STEP, CHUNK_STEP);
            scope.minX = x;
            scope.minY = y;
            scope.maxX = scope.minX + CHUNK_STEP - 1;
            scope.maxY = scope.minY + CHUNK_STEP - 1;
        }
        public void SetTile(int x, int y, TileSeen tile)
        {
            if (x >= scope.minX && y >= scope.minY && x <= scope.maxX && y <= scope.maxY)
            {
                int rx = x - scope.minX;
                int ry = y - scope.minY;
                if ((rx * CHUNK_STEP + ry) > 255)
                {
                    var c = 3;
                }
                tiles[rx * CHUNK_STEP + ry] = tile;
            }
            else
            {
                var F = 1;
                /*Log.warn("TileGroup tried to set tile out of bounds:" +
                "\n\tbounds:" + scope +
                "\n\ttarget: x:" + x + ", y:" + y);*/
            }
        }

        public TileSeen RemoveTile(int x, int y)
        {
            TileSeen tmp = GetTile(x, y);
            SetTile(x, y, null);
            return tmp;
        }

        public TileSeen GetTile(int x, int y)
        {
            if (x >= scope.minX && y >= scope.minY && x <= scope.maxX && y <= scope.maxY)
            {
                int rx = x - scope.minX;
                int ry = y - scope.minY;
                return tiles[rx * CHUNK_STEP + ry];
            }
            return null;
        }

        public bool HasTileAt(int x, int y)
        {
            return GetTile(x, y) == null;
        }
        public Rect GetScope()
        {
            return scope;
        }
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is TileGroupSeen))

                return false;
            TileGroupSeen other = (TileGroupSeen)obj;
            if (!scope.Equals(other.scope))
                return false;
            int a;
            int b;
            for (int y = 0; y < CHUNK_STEP; y++)
            {
                for (int x = 0; x < CHUNK_STEP; x++)
                {
                    if (this.tiles[x * CHUNK_STEP + y] != other.tiles[x * CHUNK_STEP + y])
                        return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.scope.minX, this.scope.maxX, this.scope.minY, this.scope.maxY);
        }
    }
}
