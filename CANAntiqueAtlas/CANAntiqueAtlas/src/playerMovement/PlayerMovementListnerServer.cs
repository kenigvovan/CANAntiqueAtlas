using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.core;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static CANAntiqueAtlas.src.core.BiomeDetectors.IBiomeDetector;
using static System.Runtime.InteropServices.JavaScript.JSType;
using CANAntiqueAtlas.src.util;
using CANAntiqueAtlas.src.network.server;
using CANAntiqueAtlas.src.core.BiomeDetectors;
using System.Threading;

namespace CANAntiqueAtlas.src.playerMovement
{
    public class PlayerMovementsListnerServer
    {
        public Dictionary<IServerPlayer, HashSet<AtlasSeenData>> PlayersAtlasses = new();
        public Dictionary<string, Vec2i> LastPlayerPos = new();
        public PlayerMovementsListnerServer()
        {

        }
        public void CheckPlayerMove(float dt)
        {
            IBiomeDetector biomeDetector = CANAntiqueAtlas.biomeDetector;
            var mapData = CANAntiqueAtlas.ServerMapInfoData.GetDimensionData();
            Dictionary<int, HashSet<Vec2i>> NewlySeenChunk = new();
            Dictionary<int, HashSet<(int, int, Tile)>> NewMapTiles = new();
            foreach (var (player, atlassesHashSet) in this.PlayersAtlasses)
            {
                if (LastPlayerPos.TryGetValue(player.PlayerUID, out var lastPos))
                {
                    //ignore if in the same plot
                    if (player.Entity.ServerPos.X / 16 == lastPos.X && player.Entity.ServerPos.Z / 16 == lastPos.Y)
                    {
                        continue;
                    }
                }

                int scanRadius = 5; //5
                int playerX = (int)player.Entity.ServerPos.X >> 4;
                int playerZ = (int)player.Entity.ServerPos.Z >> 4;
                // Look at chunks around in a circular area:
                for (double dx = -scanRadius; dx <= scanRadius; dx++)
                {
                    for (double dz = -scanRadius; dz <= scanRadius; dz++)
                    {
                        if (dx * dx + dz * dz > scanRadius * scanRadius)
                        {
                            continue; // Outside the circle
                        }
                        int x = (int)(playerX + dx);
                        x -= x % 2 == 1 ? -1 : 0;
                        
                        int z = (int)(playerZ + dz);
                        z -= z % 2 == 1 ? -1 : 0;
                        for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                Tile oldTile = mapData.GetTile(x + i, z + j);

                                BiomeType biomeId = BiomeType.NOT_FOUND;
                                if (oldTile == null)
                                {
                                    //Thread.Sleep(500);
                                    var chunkCoords = new Vec2i(x / 2, z / 2);
                                    var chunk = player.Entity.World.BlockAccessor.GetMapChunk(chunkCoords);
                                    if (chunk == null)
                                    {
                                        if (CANAntiqueAtlas.config.forceChunkLoading)
                                        {
                                            CANAntiqueAtlas.sapi.WorldManager.LoadChunkColumn(x, z);
                                            //player.worldObj.getChunkProvider().loadChunk(x << 4, z << 4);
                                            continue;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }

                                    // Scanning new chunk:
                                    //NEED TO SCAN 16X16 CHUNKS WITH CONDITIONS OF 32X32 CHUNK
                                    biomeId = biomeDetector.GetBiomeID(chunk, chunkCoords, x + i, z + j);
                                    if (biomeId != BiomeType.NOT_FOUND)
                                    {
                                        mapData.SetTile(x + i, z + j, new Tile((int)biomeId));
                                        oldTile = new Tile((int)biomeId);
                                    }
                                    //NewMapTiles.Add(oldTile);
                                }
                                if (oldTile != null)
                                {
                                    foreach (var atlas in atlassesHashSet)
                                    {
                                        if (atlas.HasTileAt(x + i, z + j))
                                        {
                                            continue;
                                        }
                                        ;
                                        if (!NewlySeenChunk.ContainsKey(atlas.key))
                                        {
                                            NewlySeenChunk[atlas.key] = new HashSet<Vec2i>();
                                        }
                                        atlas.SetTile(x + i, z + j, new TileSeen());
                                        NewlySeenChunk[atlas.key].Add(new Vec2i(x + i, z + j));
                                        if (!NewMapTiles.ContainsKey(atlas.key))
                                        {
                                            NewMapTiles[atlas.key] = new();
                                        }
                                        NewMapTiles[atlas.key].Add((x + i, z + j, oldTile));
                                    }
                                }
                            }
                        }               
                    }
                }
            }
            
            foreach (var (player, atlassesHashSet) in this.PlayersAtlasses)
            {
                Dictionary<int, HashSet<Vec2i>> tmpNewly = NewlySeenChunk.Where(kv => atlassesHashSet.Any(a => a.key == kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
                Dictionary<int, HashSet<(int, int, Tile)>> tmpNewMapTiles = NewMapTiles.Where(kv => atlassesHashSet.Any(a => a.key == kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
                if(tmpNewly.Count == 0 || tmpNewMapTiles.Count == 0)
                {
                    continue;
                }
                CANAntiqueAtlas.serverChannel.SendPacket(new NewlySeenChunksAndMapData()
                {
                    NewlySeenChunk = tmpNewly,
                    NewMapTiles = tmpNewMapTiles

                }, player);
            }
           
        }
        public void OnPlayerNowPlaying(IServerPlayer byPlayer)
        {
            if (!LastPlayerPos.ContainsKey(byPlayer.PlayerUID))
            {
                LastPlayerPos[byPlayer.PlayerUID] = new Vec2i((int)byPlayer.Entity.ServerPos.X >> 4, (int)byPlayer.Entity.ServerPos.Z >> 4);
            }
            List<int> CollectedAtlases = new();
            foreach (var inv in byPlayer.InventoryManager.Inventories)
            {
                if(inv.Value.ClassName == "creative" || inv.Value.ClassName == "ground")
                {
                    continue;
                }
                foreach (var sl in inv.Value)
                {
                    if(sl?.Itemstack?.Collectible is CANItemAtlas)
                    {
                        var atlasNumber = sl.Itemstack.Attributes.GetInt("atlasID");
                        if(atlasNumber < 0)
                        {
                            continue;
                        }
                        if (!CANAntiqueAtlas.ServerSeenChunksByAtlases.TryGetValue(atlasNumber, out var data))
                        {
                            data = new AtlasSeenData(atlasNumber);
                            CANAntiqueAtlas.ServerSeenChunksByAtlases[atlasNumber] = data;


                        }
                        if (!PlayersAtlasses.ContainsKey(byPlayer))
                        {
                            PlayersAtlasses[byPlayer] = new HashSet<AtlasSeenData>();
                        }
                        PlayersAtlasses[byPlayer].Add(data);
                        CollectedAtlases.Add(atlasNumber);
                    }
                }
            }

            var mapData = CANAntiqueAtlas.ServerMapInfoData.GetDimensionData();
            Dictionary<ShortVec2, Tile> MapCollectedTiles = new();
            foreach (var atlas in CollectedAtlases)
            {
                AtlasData atlasData = new();
                foreach(var tm in CANAntiqueAtlas.ServerSeenChunksByAtlases[atlas].GetSeenChunks())
                {
                    if(MapCollectedTiles.ContainsKey(new ShortVec2(tm.Key.x, tm.Key.y)))
                    {
                        continue;
                    }
                    var mapTile = mapData.GetTile(tm.Key.x, tm.Key.y);
                    if(mapTile != null)
                    {
                        MapCollectedTiles[new ShortVec2(tm.Key.x, tm.Key.y)] = mapTile;
                    }
                }
                CANAntiqueAtlas.serverChannel.SendPacket(new PlayerJoinedMapDataSeen()
                {
                    SeenData = CANAntiqueAtlas.ServerSeenChunksByAtlases[atlas]

                }, byPlayer);
            }
            if(MapCollectedTiles == null || MapCollectedTiles.Count == 0)
            {
                return;
            }
            CANAntiqueAtlas.serverChannel.SendPacket(new PlayerJoinedMapData()
            {
                ServerMapInfoData = MapCollectedTiles

            }, byPlayer);
        }
    }
}

