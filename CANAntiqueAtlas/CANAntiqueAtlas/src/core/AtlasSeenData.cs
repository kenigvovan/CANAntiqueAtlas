using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.network.client;
using CANAntiqueAtlas.src.util;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace CANAntiqueAtlas.src.core
{
    [ProtoContract]
    public class AtlasSeenData: ISeenTileStorage
    {
        /**
         * a map of chunks the player has seen. This map is thread-safe. CAREFUL!
         * Don't modify chunk coordinates that are already put in the map!
         * 
         * Key is a ShortVec2 representing the tilegroup's position in units of TileGroup.CHUNK_STEP
         */
        [ProtoMember(1)]
        public ConcurrentDictionary<ShortVec2, TileGroupSeen> tileGroups = new();
        [ProtoMember(2)]
        public int key;
        public AtlasSeenData()
        {

        }
        public AtlasSeenData(int key)
        {
            this.key = key;
        }
        /**
         * This function has to create a new map on each call since the packet rework
         */
        public ConcurrentDictionary<ShortVec2, TileSeen> GetSeenChunks()
        {
            ConcurrentDictionary<ShortVec2, TileSeen> chunks = new();
            TileSeen t = null;
            foreach (var entry in tileGroups)
            {

                int basex = entry.Key.x * 16;
                int basey = entry.Key.y * 16;
                for (int x = basex; x < basex + TileGroupSeen.CHUNK_STEP; x++)
                {
                    for (int y = basey; y < basey + TileGroupSeen.CHUNK_STEP; y++)
                    {
                        if(x == 31855)
                        {
                            var c = 3;
                        }
                        t = entry.Value.GetTile(x, y);
                        if (t != null)
                        {
                            chunks[new ShortVec2(x, y)] = t;
                        }
                    }
                }
            }
            return chunks;
        }
        public void SetTile(int x, int y, TileSeen tile)
        {
            ShortVec2 groupPos = new ShortVec2((int)Math.Floor(x / (float)TileGroupSeen.CHUNK_STEP),
                    (int)Math.Floor(y / (float)TileGroupSeen.CHUNK_STEP));
            if (!tileGroups.TryGetValue(groupPos, out TileGroupSeen tg))
            {
                tg = new TileGroupSeen(groupPos.x * TileGroupSeen.CHUNK_STEP, groupPos.y * TileGroupSeen.CHUNK_STEP);
                tileGroups[groupPos] = tg;
            }
            //scope.extendTo(x, y);
            tg.SetTile(x, y, tile);
        }

        /**Puts a tileGroup into this dimensionData, overwriting any previous stuff.*/
        public void PutTileGroup(TileGroupSeen t)
        {
            ShortVec2 key = new ShortVec2(t.scope.minX / TileGroupSeen.CHUNK_STEP, t.scope.minY / TileGroupSeen.CHUNK_STEP);
            tileGroups[key] = t;
        }

        public TileSeen RemoveTile(int x, int y)
        {
            //TODO
            // since scope is not modified, I assume this was never really used
            // Tile oldTile = tileGroups.remove(getKey().set(x, y));
            // if (oldTile != null) parent.markDirty();
            // return oldTile;
            return GetTile(x, y);
        }

        public TileSeen GetTile(int x, int y)
        {
            ShortVec2 groupPos = new ShortVec2((int)Math.Floor(x / (float)TileGroupSeen.CHUNK_STEP),
                    (int)Math.Floor(y / (float)TileGroupSeen.CHUNK_STEP));
            if (!tileGroups.TryGetValue(groupPos, out TileGroupSeen tg))
            {
                return null;
            }
            return tg.GetTile(x, y);
        }

        public bool HasTileAt(int x, int y)
        {
            return GetTile(x, y) != null;
        }

      
        public AtlasSeenData clone()
        {
            //TODO
            AtlasSeenData data = new AtlasSeenData(this.key);
            foreach (var kvp in tileGroups)
            {
                data.tileGroups[kvp.Key] = kvp.Value;
            }
            return data;
        }


        public override bool Equals(object obj)
        {
            if (!(obj is AtlasSeenData)) return false;
            AtlasSeenData other = (AtlasSeenData)obj;
            if (other.tileGroups.Count != tileGroups.Count) return false;
            foreach (var entry in tileGroups)
            {
                if (!this.tileGroups[entry.Key].Equals(other.tileGroups[entry.Key]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
