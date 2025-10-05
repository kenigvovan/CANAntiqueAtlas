using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.util;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Vintagestory.API.Common;
using System.Collections.Concurrent;
using Vintagestory.API.Datastructures;
using CANAntiqueAtlas.src.network.client;
using Vintagestory.API.Server;
using ProtoBuf;
using Vintagestory.API.MathTools;

namespace CANAntiqueAtlas.src.core
{
    /** All tiles seen in dimension. Thread-safe (probably) */
    [ProtoContract]
    public class DimensionData: ITileStorage
    {
        public AtlasData parent;
	    public int dimension;

        [ProtoMember(1)]
        private int browsingX;
        [ProtoMember(2)]
        private int browsingY;
        [ProtoMember(3)]
        private double browsingZoom = 0.5;

        /**
         * a map of chunks the player has seen. This map is thread-safe. CAREFUL!
         * Don't modify chunk coordinates that are already put in the map!
         * 
         * Key is a FastVec2i representing the tilegroup's position in units of TileGroup.CHUNK_STEP
         */
        [ProtoMember(4)]
        private ConcurrentDictionary<FastVec2i, TileGroup> tileGroups = new();
        /** Limits of explored area, in chunks. */
        private Rect scope = new Rect();
        public DimensionData()
        {

        }
        public DimensionData(AtlasData parent)
        {
            this.parent = parent;
        }

        /**
         * This function has to create a new map on each call since the packet rework
         */
        public ConcurrentDictionary<FastVec2i, Tile> GetSeenChunks()
        {
            ConcurrentDictionary<FastVec2i, Tile> chunks = new ConcurrentDictionary<FastVec2i, Tile>();
            Tile t = null;
            foreach (var entry in tileGroups)
            {
                int basex = entry.Key.X;
                int basey = entry.Key.Y;
                for (int x = basex; x < basex + TileGroup.CHUNK_STEP; x++)
                {
                    for (int y = basey; y < basey + TileGroup.CHUNK_STEP; y++)
                    {
                        t = entry.Value.GetTile(x, y);
                        if (t != null)
                        {
                            chunks[new FastVec2i(x, y)] = t;
                        }
                    }
                }
            }
            return chunks;
        }

        /** Set world coordinates that are in the center of the GUI. */
        public void setBrowsingPosition(int x, int y, double zoom)
        {
            this.browsingX = x;
            this.browsingY = y;
            this.browsingZoom = zoom;
            if (browsingZoom <= 0)
            {
                //Log.warn("Setting map zoom to invalid value of %f", zoom);
                browsingZoom = CANAntiqueAtlas.config.minScale;
            }
            //parent.Save();
        }

        public int getBrowsingX()
        {
            return browsingX;
        }

        public int getBrowsingY()
        {
            return browsingY;
        }

        public double getBrowsingZoom()
        {
            return browsingZoom;
        }
        public void SetTile(int x, int y, Tile tile)
        {
            FastVec2i groupPos = new FastVec2i((int)Math.Floor(x / (float)TileGroup.CHUNK_STEP),
                    (int)Math.Floor(y / (float)TileGroup.CHUNK_STEP));
            if(!tileGroups.TryGetValue(groupPos, out TileGroup tg))
            {
                tg = new TileGroup(groupPos.X * TileGroup.CHUNK_STEP, groupPos.Y * TileGroup.CHUNK_STEP);
                tileGroups[groupPos] = tg;
            }
            //scope.extendTo(x, y);
            tg.SetTile(x, y, tile);
            //parent.Save();
        }

        /**Puts a tileGroup into this dimensionData, overwriting any previous stuff.*/
        public void PutTileGroup(TileGroup t)
        {
            FastVec2i key = new FastVec2i(t.scope.minX / TileGroup.CHUNK_STEP, t.scope.minY / TileGroup.CHUNK_STEP);
            tileGroups[key] = t;
        }

        public Tile RemoveTile(int x, int y)
        {
            //TODO
            // since scope is not modified, I assume this was never really used
            // Tile oldTile = tileGroups.remove(getKey().set(x, y));
            // if (oldTile != null) parent.markDirty();
            // return oldTile;
            return GetTile(x, y);
        }

        public Tile GetTile(int x, int y)
        {
            FastVec2i groupPos = new FastVec2i((int)Math.Floor(x / (float)TileGroup.CHUNK_STEP),
                    (int)Math.Floor(y / (float)TileGroup.CHUNK_STEP));
            if(!tileGroups.TryGetValue(groupPos, out TileGroup tg))
            {
                return null;
            }
            return tg.GetTile(x, y);
        }

        public bool HasTileAt(int x, int y)
        {
            return GetTile(x, y) != null;
        }

        public Rect GetScope()
        {
            return scope;
        }
        public DimensionData clone()
        {
            //TODO
            DimensionData data = new DimensionData(parent);
            foreach (var kvp in tileGroups)
            {
                data.tileGroups[kvp.Key] = kvp.Value;
            }
            data.scope.set(scope);
            return data;
        }

        public void syncOnPlayer(int atlasID, EntityPlayer player)
        {
            //Log.info("Sending dimension #%d", dimension);
            List<TileGroup> tgs = new List<TileGroup>(TileGroupsPacket.TILE_GROUPS_PER_PACKET);
            int count = 0;
            int total = 0;
            foreach (var t in tileGroups)
            {
                tgs.Add(t.Value);
                count++;
                total++;
                if (count >= TileGroupsPacket.TILE_GROUPS_PER_PACKET)
                {
                    TileGroupsPacket p = new TileGroupsPacket { tileGroups = tgs, atlasID = atlasID };
                    CANAntiqueAtlas.serverChannel.SendPacket(
                             p
                           , player.Player as IServerPlayer);
                    count = 0;
                }
            }
            if (count > 0)
            {
                TileGroupsPacket p = new TileGroupsPacket { tileGroups = tgs, atlasID = atlasID };
                CANAntiqueAtlas.serverChannel.SendPacket(
                            p
                          , player.Player as IServerPlayer);
            }
            //Log.info("Sent dimension #%d (%d tiles)", dimension, total);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DimensionData)) return false;
            DimensionData other = (DimensionData)obj;
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
