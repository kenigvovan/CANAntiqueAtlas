using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.util;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using ProtoBuf;
using System.Xml.Linq;
using Vintagestory.API.MathTools;

namespace CANAntiqueAtlas.src.core
{
    /** Represents a group of tiles that may be sent/stored as a single attribute */
    [ProtoContract]
    public class TileGroup: ITileStorage
    {
	    /** The width/height of this TileGroup */
	    public static int CHUNK_STEP = 16;

        /** The area of chunks this group covers */
        [ProtoMember(2)]
        public Rect scope = new Rect(0, 0, CHUNK_STEP, CHUNK_STEP);

        /** The tiles in this scope */
        [ProtoMember(1)]
        public Dictionary<long, Tile> tiles = new();
        public TileGroup()
        {
            
        }
        public TileGroup(int x, int y)
        {
            scope.minX = x;
            scope.minY = y;
            scope.maxX = scope.minX + CHUNK_STEP - 1;
            scope.maxY = scope.minY + CHUNK_STEP - 1;
        }
        public void SetTile(int x, int y, Tile tile)
        {
            if (x >= scope.minX && y >= scope.minY && x <= scope.maxX && y <= scope.maxY)
            {
                int rx = x - scope.minX;
                int ry = y - scope.minY;
                tiles[(uint)((uint)rx + ((long)ry << 16))] = tile;
            }
        }

        public Tile RemoveTile(int x, int y)
        {
            Tile tmp = GetTile(x, y);
            SetTile(x, y, null);
            return tmp;
        }

        public Tile GetTile(int x, int y)
        {
            if (x >= scope.minX && y >= scope.minY && x <= scope.maxX && y <= scope.maxY)
            {
                int rx = x - scope.minX;
                int ry = y - scope.minY;
                tiles.TryGetValue((uint)((uint)rx + ((long)ry << 16)), out Tile tile);
                return tile;
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
            if (obj == null || !(obj is TileGroup))
            {
                return false;
            }
            if (!this.tiles.Equals(obj as TileGroup))
            {
                return false;
            }
            return true;
            /*TileGroup other = (TileGroup)obj;
            if (!scope.Equals(other.scope))
                return false;
            int a;
            int b;
            for (int y = 0; y < CHUNK_STEP; y++)
            {
                for (int x = 0; x < CHUNK_STEP; x++)
                {
                    a = (this.tiles[x * CHUNK_STEP + y] == null) ? -1 : this.tiles[x * CHUNK_STEP + y].biomeID;
                    b = (other.tiles[x * CHUNK_STEP + y] == null) ? -1 : other.tiles[x * CHUNK_STEP + y].biomeID;
                    if (a != b)
                        return false;
                }
            }
            return true;*/
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.scope.minX, this.scope.maxX, this.scope.minY, this.scope.maxY);
        }
    }
}
