using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Vintagestory.API.Server;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using CANAntiqueAtlas.src.util;
using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using static System.Formats.Asn1.AsnWriter;
using Vintagestory.GameContent;
using System.Collections.Concurrent;
using CANAntiqueAtlas.src.network.server;
using Newtonsoft.Json;
using System.Security.Claims;
using CANAntiqueAtlas.src.network.client;
using ProtoBuf;
using System.IO;

namespace CANAntiqueAtlas.src.core
{
    [ProtoContract]
    public class AtlasData
    {
        public static  int VERSION = 3;
        public static  string TAG_VERSION = "aaVersion";
	    public static  string TAG_DIMENSION_MAP_LIST = "qDimensionMap";
	    public static  string TAG_DIMENSION_ID = "qDimensionID";
	    public static  string TAG_VISITED_CHUNKS = "qVisitedChunks";
	
	    // Navigation
	    public static  string TAG_BROWSING_X = "qBrowseX";
	    public static  string TAG_BROWSING_Y = "qBrowseY";
	    public static  string TAG_BROWSING_ZOOM = "qBrowseZoom";
        public static string TAG_KEY = "qKey";
        /** This map contains, for each dimension, a map of chunks the player
         * has seen. This map is thread-safe.
         * CAREFUL! Don't modify chunk coordinates that are already put in the map! */
        //We only have 1 dimension
        [ProtoMember(1)]
        private DimensionData dimensionData;
        [ProtoMember(2)]
        string key;
        public AtlasData()
        {
            var c = 3;
        }
        public AtlasData(string key)
        {
            this.key = key;
            dimensionData = new DimensionData(this);
        }
        /** Set of players this Atlas data has been sent to. */
        private HashSet<EntityPlayer> playersSentTo = new HashSet<EntityPlayer>();

        /* TODO: Packet Rework
	     *  Dimension data should check the server for updates*/
        /** If this dimension is not yet visited, empty DimensionData will be created. */
        public DimensionData GetDimensionData()
        {
            if (dimensionData == null)
            {
                dimensionData = new DimensionData(this);
            }
            return dimensionData;
        }
        public void Save()
        {
            CANAntiqueAtlas.sapi.WorldManager.SaveGame.StoreData(key, this);
        }
        /** Puts a given tile into given map at specified coordinates and,
         * if tileStitcher is present, sets appropriate sectors on adjacent tiles. */
        public void SetTile(int dimension, int x, int y, Tile tile)
        {
            DimensionData dimData = this.dimensionData;
            dimData.SetTile(x, y, tile);
        }

        /** Returns the Tile previously set at given coordinates. */
        public Tile RemoveTile(int dimension, int x, int y)
        {
            DimensionData dimData = this.dimensionData;
            return dimData.RemoveTile(x, y);
        }

        public HashSet<int> GetVisitedDimensions()
        {
            return new HashSet<int> { 0 };
            ;
        }
        public ConcurrentDictionary<ShortVec2, Tile> GetSeenChunksInDimension(int dimension)
        {
            return this.dimensionData.GetSeenChunks();
        }

        /** The set of players this AtlasData has already been sent to. */
        public ICollection<EntityPlayer> getSyncedPlayers()
        {
            return playersSentTo;
        }
        /** Whether this AtlasData has already been sent to the specified player. */
        public bool isSyncedOnPlayer(EntityPlayer player)
        {
            return playersSentTo.Contains(player);
        }

        /** Send all data to the player in several zipped packets. Called once
         * during the first run of ItemAtals.onUpdate(). */
        public void syncOnPlayer(int atlasID, EntityPlayer player)
        {
            CANAntiqueAtlas.serverChannel.SendPacket(
                           new MapDataPacket()
                           {
                               atlasID = atlasID,
                               data = this
                           }
                           , player as IServerPlayer);

            this.dimensionData.syncOnPlayer(atlasID, player);
            
            //Log.info("Sent Atlas #%d data to player %s", atlasID, player.getCommandSenderName());
            playersSentTo.Add(player);
        }

        public bool isEmpty()
        {
            return false;
        }

        public bool Equals(object obj)
        {
            if (!(obj is AtlasData)) return false;
            AtlasData other = (AtlasData)obj;
            if(!this.dimensionData.Equals(obj))
            {
                return false;
            }
            //if (other.dimensionMap.size() != dimensionMap.size()) return false;
            /*for (int key: dimensionMap.keySet())
            {
                if (!dimensionMap.get(key).equals(other.dimensionMap.get(key))) return false;
            }*/
            return true;
        }
    }
}
