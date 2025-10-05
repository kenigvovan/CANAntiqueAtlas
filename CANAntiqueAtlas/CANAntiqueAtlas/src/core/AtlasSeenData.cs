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
using Vintagestory.API.MathTools;
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
         * Key is a FastVec2i representing the tilegroup's position in units of TileGroup.CHUNK_STEP
         */
        [ProtoMember(1)]
        public ConcurrentDictionary<FastVec2i, TileGroupSeen> tileGroups = new();
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
        public ConcurrentDictionary<FastVec2i, bool> GetSeenChunks()
        {
            ConcurrentDictionary<FastVec2i, bool> chunks = new();
            bool t;
            foreach (var entry in tileGroups)
            {

                int basex = entry.Key.X * 16;
                int basey = entry.Key.Y * 16;
                for (int x = basex; x < basex + TileGroupSeen.CHUNK_STEP; x++)
                {
                    for (int y = basey; y < basey + TileGroupSeen.CHUNK_STEP; y++)
                    {
                        t = entry.Value.GetTile(x, y);
                        if (t)
                        {
                            chunks[new FastVec2i(x, y)] = t;
                        }
                    }
                }
            }
            return chunks;
        }
        public void SetTile(int x, int y, bool tile)
        {
            FastVec2i groupPos = new FastVec2i((int)Math.Floor(x / (float)TileGroupSeen.CHUNK_STEP),
                    (int)Math.Floor(y / (float)TileGroupSeen.CHUNK_STEP));
            if (!tileGroups.TryGetValue(groupPos, out TileGroupSeen tg))
            {
                tg = new TileGroupSeen(groupPos.X * TileGroupSeen.CHUNK_STEP, groupPos.Y * TileGroupSeen.CHUNK_STEP);
                tileGroups[groupPos] = tg;
            }
            //scope.extendTo(x, y);
            tg.SetTile(x, y, tile);
        }

        /**Puts a tileGroup into this dimensionData, overwriting any previous stuff.*/
        public void PutTileGroup(TileGroupSeen t)
        {
            FastVec2i key = new FastVec2i(t.scope.minX / TileGroupSeen.CHUNK_STEP, t.scope.minY / TileGroupSeen.CHUNK_STEP);
            tileGroups[key] = t;
        }

        public bool RemoveTile(int x, int y)
        {
            //TODO
            // since scope is not modified, I assume this was never really used
            // Tile oldTile = tileGroups.remove(getKey().set(x, y));
            // if (oldTile != null) parent.markDirty();
            // return oldTile;
            return GetTile(x, y);
        }

        public bool GetTile(int x, int y)
        {
            FastVec2i groupPos = new FastVec2i((int)Math.Floor(x / (float)TileGroupSeen.CHUNK_STEP),
                    (int)Math.Floor(y / (float)TileGroupSeen.CHUNK_STEP));
            if (!tileGroups.TryGetValue(groupPos, out TileGroupSeen tg))
            {
                return false;
            }
            return tg.GetTile(x, y);
        }

        public bool HasTileAt(int x, int y)
        {
            return GetTile(x, y);
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
