using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CANAntiqueAtlas.src;
using CANAntiqueAtlas.src.core;
using CANAntiqueAtlas.src.core.BiomeDetectors;
using CANAntiqueAtlas.src.gui;
using CANAntiqueAtlas.src.gui.Map.TileLayer;
using CANAntiqueAtlas.src.gui.render;
using CANAntiqueAtlas.src.harmony;
using CANAntiqueAtlas.src.item;
using CANAntiqueAtlas.src.network.client;
using CANAntiqueAtlas.src.network.server;
using CANAntiqueAtlas.src.playerMovement;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

using Vintagestory.GameContent;
using static CANAntiqueAtlas.src.core.BiomeDetectors.IBiomeDetector;

namespace CANAntiqueAtlas
{
    public class CANAntiqueAtlas : ModSystem
    {
        public static Harmony harmonyInstance;
        public const string harmonyID = "canantiqueatlas.Patches";
        public static AtlasDataHandler atlasData = new AtlasDataHandler();
        public const string ServerMapInfoDataStringKey = "ServerMapInfoDataStringKey";
        public static AtlasData ServerMapInfoData = new("here");
        public static AtlasData ClientMapInfoData = new("here");
        public static long LastAtlasId = -1;
        public const string ServerSeenChunksByAtlasStringKey = "ServerSeenChunksByAtlasStringKey";

        public static ConcurrentDictionary<long, AtlasSeenData> ServerSeenChunksByAtlases = new();
        public static ConcurrentDictionary<long, AtlasSeenData> ClientSeenChunksByAtlases = new();
        public const string WorldAtlasDataIdString = "WorldAtlasDataIdString";
        public static AtlasData atlasD;
        public static Config config;
        public static ICoreServerAPI sapi;
        public static ICoreClientAPI capi;
        internal static IServerNetworkChannel serverChannel;
        internal static IClientNetworkChannel clientChannel;
        public static IBiomeDetector biomeDetector;
        public TextureSetMap textureSetMap;
        public static BiomeTextureMap biomeTextureMap;
        public PlayerMovementsListnerServer pmls;
        public static Dictionary<string, int> BiomesMapping = new();
        public override void Start(ICoreAPI api)
        {
            if (api.Side == EnumAppSide.Client)
            {
                capi = api as ICoreClientAPI;
            }
            else
            {
                sapi = api as ICoreServerAPI;
            }
            api.RegisterItemClass("CANItemAtlas", typeof(CANItemAtlas));
            api.RegisterItemClass("CANItemEmptyAtlas", typeof(CANItemEmptyAtlas));
            harmonyInstance = new Harmony(harmonyID);
            //harmonyInstance.Patch(typeof(WorldMapManager).GetMethod("ShouldLoad", new Type[] { typeof(EnumAppSide) }), prefix: new HarmonyMethod(typeof(harmPatches).GetMethod("Prefix_ModSystemShouldLoad")));
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            loadConfig(api);
            serverChannel = sapi.Network.RegisterChannel("canantiqueatlas");
            serverChannel.RegisterMessageType(typeof(TileGroupsPacket));
            serverChannel.RegisterMessageType(typeof(NewlySeenChunksAndMapData));
            serverChannel.RegisterMessageType(typeof(PlayerJoinedMapData));
            serverChannel.RegisterMessageType(typeof(PlayerJoinedMapDataSeen));
            serverChannel.RegisterMessageType(typeof(DropedAtlasIdPacket));
            serverChannel.RegisterMessageType(typeof(SyncBiomesPacket));
            serverChannel.RegisterMessageType(typeof(ConfigUpdateValuesPacket));
            serverChannel.SetMessageHandler<DropedAtlasIdPacket>((player, packet) =>
            {
                pmls.ResyncPlayerAtlases(player);
            });
            biomeDetector = new BiomeDetectorBase();
            pmls = new PlayerMovementsListnerServer();
            sapi.Event.RegisterGameTickListener(pmls.CheckPlayerMove, 2000);
            sapi.Event.PlayerNowPlaying += pmls.OnPlayerNowPlaying;
            api.Event.PlayerJoin += SendUpdatedConfigValues;
            LoadDefaultBiomeConditions(api);
            LoadFinalSelectionPriority(api);
            api.ChatCommands.Create("canatlas")
               .RequiresPlayer()
               .RequiresPrivilege(Privilege.controlserver)
               .BeginSub("shst")
                   .HandleWith(ShowStatsCommand)
                .EndSub()
                .BeginSub("clearall")
                   .HandleWith(ClearAllAtlasDataCommand)
                .EndSub()
                .BeginSub("setbiome")
                   .WithDescription("Set biome for chunks around current position. Usage: /canatlas setbiome <biomeId|biomeName> [radius]")
                   .WithArgs(api.ChatCommands.Parsers.Word("biome"), api.ChatCommands.Parsers.OptionalInt("radius", 1))
                   .HandleWith(SetChunkBiomeCommand)
                .EndSub()
                .BeginSub("biomes")
                   .WithDescription("List all available biomes and their IDs")
                   .HandleWith(ListBiomesCommand)
                .EndSub();
            sapi.Event.ServerRunPhase(EnumServerRunPhase.ModsAndConfigReady, () =>
            {
                ServerSeenChunksByAtlases =  CANAntiqueAtlas.sapi.WorldManager.SaveGame.GetData<ConcurrentDictionary<long, AtlasSeenData>>(ServerSeenChunksByAtlasStringKey);
                if(ServerSeenChunksByAtlases == null)
                {
                    ServerSeenChunksByAtlases = new();
                }
                ServerMapInfoData = CANAntiqueAtlas.sapi.WorldManager.SaveGame.GetData<AtlasData>(ServerMapInfoDataStringKey);
                if (ServerMapInfoData == null)
                {
                    ServerMapInfoData = new();
                }
                CANItemAtlas.WORLD_ATLAS_DATA_ID = CANAntiqueAtlas.sapi.WorldManager.SaveGame.GetData<long>(WorldAtlasDataIdString);
            });
            sapi.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, () =>
            {
                CANAntiqueAtlas.sapi.WorldManager.SaveGame.StoreData(ServerSeenChunksByAtlasStringKey, ServerSeenChunksByAtlases);
                CANAntiqueAtlas.sapi.WorldManager.SaveGame.StoreData<AtlasData>(ServerMapInfoDataStringKey, ServerMapInfoData);
            });
            sapi.Event.Timer(() =>
            {
                lock (ServerSeenChunksByAtlases)
                {
                    CANAntiqueAtlas.sapi.WorldManager.SaveGame.StoreData(ServerSeenChunksByAtlasStringKey, ServerSeenChunksByAtlases);
                }
                lock (ServerMapInfoData)
                {
                    CANAntiqueAtlas.sapi.WorldManager.SaveGame.StoreData<AtlasData>(ServerMapInfoDataStringKey, ServerMapInfoData);
                }                
            }, config.SaveMapChunksEveryNSeconds);
            if (!config.allowCoordinateHud)
            {
                sapi.World.Config.SetBool("allowCoordinateHud", false);
            }
            else
            {
                sapi.World.Config.SetBool("allowCoordinateHud", true);
            }
            if (!config.allowMap)
            {
                sapi.World.Config.SetBool("allowMap", false);
            }
            else
            {
                sapi.World.Config.SetBool("allowMap", true);
            }
            sapi.Event.PlayerNowPlaying += SendBiomeMapping;
        }
        public static void SendUpdatedConfigValues(IServerPlayer player)
        {
            serverChannel.SendPacket(
                   new ConfigUpdateValuesPacket()
                   {
                       showPlayerMarker = config.showPlayerMarker,
                   }
                   , player);
        }
        public void SendBiomeMapping(IServerPlayer byPlayer)
        {
            serverChannel.SendPacket(new SyncBiomesPacket
            {
                BiomesMapping = CANAntiqueAtlas.BiomesMapping
            }, byPlayer);
        }
        public static TextCommandResult ShowStatsCommand(TextCommandCallingArgs args)
        {
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            ClimateCondition cond = CANAntiqueAtlas.sapi.World.BlockAccessor.GetClimateAt(player.Entity.ServerPos.AsBlockPos, EnumGetClimateMode.WorldGenValues);
            StringBuilder stringBuilder = new StringBuilder();
           
            stringBuilder.AppendLine(string.Format("temp: {0}, rain: {1}, fert: {2}, forest: {3}, shrub: {4}", cond.Temperature, cond.Rainfall, cond.Fertility, cond.ForestDensity, cond.ShrubDensity));
            Console.WriteLine(stringBuilder.ToString());
            tcr.StatusMessage = stringBuilder.ToString();
            return tcr;
        }
        public static TextCommandResult ClearAllAtlasDataCommand(TextCommandCallingArgs args)
        {
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            ServerSeenChunksByAtlases.Clear();
            ServerMapInfoData = new();
            //tcr.StatusMessage = stringBuilder.ToString();
            return tcr;
        }
        public static TextCommandResult SetChunkBiomeCommand(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            string biomeArg = (string)args.Parsers[0].GetValue();

            // Resolve biome ID from name or numeric string
            int biomeId;
            if (!int.TryParse(biomeArg, out biomeId))
            {
                var match = BiomesMapping.FirstOrDefault(kv => string.Equals(kv.Key, biomeArg, StringComparison.OrdinalIgnoreCase));
                biomeId = match.Value;
                if (match.Key == null)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Unknown biome name: " + biomeArg);
                    sb.AppendLine("Available biomes:");
                    foreach (var kv in BiomesMapping)
                    {
                        sb.AppendLine($"  {kv.Key} (id: {kv.Value})");
                    }
                    return TextCommandResult.Error(sb.ToString());
                }
            }

            int radius = (int)args.Parsers[1].GetValue();
            int centerX = (int)player.Entity.ServerPos.X >> 4;
            int centerZ = (int)player.Entity.ServerPos.Z >> 4;

            var updatedTiles = new Dictionary<FastVec2i, Tile>();
            lock (ServerMapInfoData)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        if (dx * dx + dz * dz > radius * radius) continue;
                        int x = centerX + dx;
                        int z = centerZ + dz;
                        Tile newTile = new Tile(biomeId);
                        ServerMapInfoData.SetTile(0, x, z, newTile);
                        updatedTiles[new FastVec2i(x, z)] = newTile;
                    }
                }
            }

            // Send updated tiles to all online players
            foreach (IPlayer onlinePlayer in sapi.World.AllOnlinePlayers)
            {
                serverChannel.SendPacket(new PlayerJoinedMapData()
                {
                    ServerMapInfoData = updatedTiles
                }, onlinePlayer as IServerPlayer);
            }

            return TextCommandResult.Success($"Set {updatedTiles.Count} chunks around ({centerX}, {centerZ}) with radius {radius} to biome {biomeId}" +
                (BiomesMapping.ContainsValue(biomeId) ? "" : " (warning: unknown biome ID)"));
        }
        public static TextCommandResult ListBiomesCommand(TextCommandCallingArgs args)
        {
            if (BiomesMapping.Count == 0)
            {
                return TextCommandResult.Success("No biomes registered.");
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Available biomes:");
            foreach (var kv in BiomesMapping.OrderBy(kv => kv.Value))
            {
                sb.AppendLine($"  {kv.Value}: {kv.Key}");
            }
            return TextCommandResult.Success(sb.ToString());
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            //Environment.SetEnvironmentVariable("CAIRO_DEBUG_DISPOSE", "1");
            capi = api;
            loadConfig(api);
            clientChannel = api.Network.RegisterChannel("canantiqueatlas");
            clientChannel.RegisterMessageType(typeof(TileGroupsPacket));
            clientChannel.RegisterMessageType(typeof(NewlySeenChunksAndMapData));
            clientChannel.RegisterMessageType(typeof(PlayerJoinedMapData));
            clientChannel.RegisterMessageType(typeof(PlayerJoinedMapDataSeen));
            clientChannel.RegisterMessageType(typeof(DropedAtlasIdPacket));
            clientChannel.RegisterMessageType(typeof(SyncBiomesPacket));
            clientChannel.RegisterMessageType(typeof(ConfigUpdateValuesPacket));
            clientChannel.SetMessageHandler<TileGroupsPacket>((packet) =>
            {
                AtlasData atlasData = CANAntiqueAtlas.atlasData.GetAtlasData(packet.atlasID, capi.World);
                DimensionData dimData = atlasData.GetDimensionData();
                foreach (TileGroup t in packet.tileGroups)
                {
                    dimData.PutTileGroup(t);
                }
            });
            clientChannel.SetMessageHandler<NewlySeenChunksAndMapData>((packet) =>
            {
                lock (ClientMapInfoData)
                {
                    //general data
                    foreach (HashSet<(int, int, Tile)> hs in packet.NewMapTiles.Values)
                    {
                        foreach (var t in hs)
                        {
                            ClientMapInfoData.SetTile(0, t.Item1, t.Item2, t.Item3);
                        }
                    }
                }
                
                lock (ClientSeenChunksByAtlases)
                {
                    foreach (var (atlasId, hs) in packet.NewlySeenChunk)
                    {
                        if (ClientSeenChunksByAtlases.TryGetValue(atlasId, out var asd))
                        {
                            foreach (var t in hs)
                            {
                                asd.SetTile(t.X, t.Y, true);
                            }
                        }
                        else
                        {
                            var asdNew = new AtlasSeenData(atlasId);
                            foreach (var t in hs)
                            {
                                asdNew.SetTile(t.X, t.Y, true);
                            }
                            ClientSeenChunksByAtlases[atlasId] = asdNew;
                        }
                    }
                }
                (capi.ModLoader.GetModSystem<CANWorldMapManager>()?.MapLayers[0] as CANChunkMapLayer)?.Event_OnChunkDataReceived(packet.NewMapTiles);
            });
            clientChannel.SetMessageHandler<PlayerJoinedMapDataSeen>((packet) =>
            {
                ClientSeenChunksByAtlases[packet.SeenData.key] = packet.SeenData;
            });
            clientChannel.SetMessageHandler<PlayerJoinedMapData>((packet) =>
            {
                foreach(var it in packet.ServerMapInfoData)
                {
                    ClientMapInfoData.SetTile(0, it.Key.X, it.Key.Y, it.Value);
                }
            });
            clientChannel.SetMessageHandler<SyncBiomesPacket>((packet) =>
            {
                var defaultMapping = new List<(string, TextureSet)>()
                {
                    ("Glacier", TextureSet.SNOW),
                    ("Water", TextureSet.WATER),
                    ("Swamp", TextureSet.SWAMP),
                    ("TemperateForest", TextureSet.TEMPERATEFOREST),
                    ("Desert", TextureSet.DESERT),
                    ("Plains", TextureSet.PLAINS),
                    ("Plateau", TextureSet.PLATEAU_MESA_TREES),
                    ("Mountains", TextureSet.MOUNTAINS),
                    ("MountainsSnowCaps", TextureSet.MOUNTAINS_SNOW_CAPS),
                    ("SparseForest", TextureSet.SPARSE_FOREST),
                    ("Hills", TextureSet.HILLS),
                    ("HotSpring", TextureSet.LAVA),
                    ("Redwood", TextureSet.MEGA_SPRUCE),
                    ("Jungle", TextureSet.JUNGLE)
                };
                foreach(var (name, texSet) in defaultMapping)
                {
                    if(packet.BiomesMapping.TryGetValue(name, out var biomeId))
                    {
                        biomeTextureMap.setTexture(biomeId, texSet);
                    }
                }
                /*biomeTextureMap.setTexture((int)BiomeType.Glacier, TextureSet.SNOW);
                biomeTextureMap.setTexture((int)BiomeType.Water, TextureSet.WATER);
                biomeTextureMap.setTexture((int)BiomeType.Swamp, TextureSet.SWAMP);
                biomeTextureMap.setTexture((int)BiomeType.TemperateForest, TextureSet.TEMPERATEFOREST);
                biomeTextureMap.setTexture((int)BiomeType.Desert, TextureSet.DESERT);
                biomeTextureMap.setTexture((int)BiomeType.Plains, TextureSet.PLAINS);
                biomeTextureMap.setTexture((int)BiomeType.Plateau, TextureSet.PLATEAU_MESA_TREES);
                biomeTextureMap.setTexture((int)BiomeType.Mountains, TextureSet.MOUNTAINS);
                biomeTextureMap.setTexture((int)BiomeType.MountainsSnowCaps, TextureSet.MOUNTAINS_SNOW_CAPS);
                biomeTextureMap.setTexture((int)BiomeType.SparseForest, TextureSet.SPARSE_FOREST);
                biomeTextureMap.setTexture((int)BiomeType.Hills, TextureSet.HILLS);
                biomeTextureMap.setTexture((int)BiomeType.HotSpring, TextureSet.LAVA);
                biomeTextureMap.setTexture((int)BiomeType.Redwood, TextureSet.MEGA_SPRUCE);
                biomeTextureMap.setTexture((int)BiomeType.Jungle, TextureSet.JUNGLE);*/
            });
            clientChannel.SetMessageHandler<ConfigUpdateValuesPacket>((packet) =>
            {
                config.showPlayerMarker = packet.showPlayerMarker;

            });
            textureSetMap = TextureSetMap.instance();
            //registerDefaultTextureSets(textureSetMap);
            biomeTextureMap = BiomeTextureMap.instance();
            if (config.inputDefaultTextureSets)
            {
                
            }        
        }
        private void loadConfig(ICoreAPI api)
        {        
            config = api.LoadModConfig<Config>(this.Mod.Info.ModID + ".json");
               
            if (config == null)
            {
                config = new Config();
            }
            api.World.Config.SetBool("EnableAtlasCopy", config.EnableAtlasCopy);
            if (api.Side == EnumAppSide.Server)
            {
                api.StoreModConfig<Config>(config, this.Mod.Info.ModID + ".json");
            }       
            return;          
        }
        private int GetBiomeId(BiomeConditions cond)
        {
            if (CANAntiqueAtlas.BiomesMapping.TryGetValue(cond.BiomeName, out var existingBiome))
            {
                return existingBiome;
            }
            else
            {
                int newId = CANAntiqueAtlas.BiomesMapping.Count + 1;
                CANAntiqueAtlas.BiomesMapping[cond.BiomeName] = newId;
                return newId;
            }
        }
        private void LoadDefaultBiomeConditions(ICoreServerAPI api)
        {
            bool forceDefault = false;
            if(this.Mod.Info.NetworkVersion == "1.0.8" && !config.configMigrated)
            {
                forceDefault = true;
                config.configMigrated = true;
                api.StoreModConfig<Config>(config, this.Mod.Info.ModID + ".json");
            }
            string folderPath = Path.Combine(GamePaths.ModConfig, "canantiqueatlas");
            if (forceDefault || !Directory.Exists(folderPath) || !File.Exists(Path.Combine(folderPath, "default_bioms.json")))
            {
                Directory.CreateDirectory(folderPath);
                Dictionary<AssetLocation, JToken> many = api.Assets.GetMany<JToken>(api.Server.Logger, "config/conditions/default_bioms");

                foreach (KeyValuePair<AssetLocation, JToken> val in many)
                {
                    if (val.Value is JObject)
                    {
                        BiomeConditions conditions = val.Value.ToObject<BiomeConditions>();
                        conditions.BiomeId = GetBiomeId(conditions);
                        biomeDetector.RegisterBiome(conditions);
                        File.WriteAllText(Path.Combine(folderPath, "default_bioms"), val.Value.ToString(Newtonsoft.Json.Formatting.Indented));
                    }
                    if (val.Value is JArray)
                    {
                        foreach (JToken token in (val.Value as JArray))
                        {
                            BiomeConditions conditions = token.ToObject<BiomeConditions>();
                            conditions.BiomeId = GetBiomeId(conditions);
                            biomeDetector.RegisterBiome(conditions);
                        }
                        File.WriteAllText(Path.Combine(folderPath, "default_bioms.json"), val.Value.ToString(Newtonsoft.Json.Formatting.Indented));
                    }

                }
            }
            else
            {
                string[] jsonFiles = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly);

                foreach (string file in jsonFiles)
                {
                    string jsonText = File.ReadAllText(file);
                    try
                    {
                        JToken token = JToken.Parse(jsonText);

                        if (token is JObject obj)
                        {
                            BiomeConditions conditions = token.ToObject<BiomeConditions>();
                            conditions.BiomeId = GetBiomeId(conditions);
                            biomeDetector.RegisterBiome(conditions);
                        }
                        else if (token is JArray arr)
                        {
                            foreach (JToken innerToken in (token as JArray))
                            {
                                BiomeConditions conditions = innerToken.ToObject<BiomeConditions>();
                                conditions.BiomeId = GetBiomeId(conditions);
                                biomeDetector.RegisterBiome(conditions);
                            }
                        }
                        else
                        {

                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }
        private void LoadFinalSelectionPriority(ICoreServerAPI api)
        {
            string folderPath = Path.Combine(GamePaths.ModConfig, "canantiqueatlas");
            if (File.Exists(Path.Combine(folderPath, "default_final_selection_priority.json")))
            {
                string jsonText = File.ReadAllText(Path.Combine(folderPath, "default_final_selection_priority.json"));
                //File.WriteAllText(Path.Combine(folderPath, "default_final_selection_priority.json"), JsonSerializer.Serialize(new Dictionary<int, float>() { { 1, 0.1f }, { 2, 2f } }));
                var dict = JsonSerializer.Deserialize<Dictionary<string, float>>(jsonText);
                Dictionary<int, float> priorityDict = new();
                foreach(var it in dict)
                {
                    if(CANAntiqueAtlas.BiomesMapping.TryGetValue(it.Key, out var biomeId))
                    {
                        priorityDict[biomeId] = it.Value;
                    }
                }
                biomeDetector.AddToFinalPriorityMap(priorityDict);
            }
            else
            {
                var tmpDict = new Dictionary<string, float>() {
                        {"HotSpring", 1 },
                        {"Redwood", 0.99f },
                        {"Jungle", 0.99f }
                    };
                File.WriteAllText(Path.Combine(folderPath, "default_final_selection_priority.json"),
                    JsonSerializer.Serialize(tmpDict));
                Dictionary<int, float> priorityDict = new();
                foreach (var it in tmpDict)
                {
                    if (CANAntiqueAtlas.BiomesMapping.TryGetValue(it.Key, out var biomeId))
                    {
                        priorityDict[biomeId] = it.Value;
                    }
                }
                biomeDetector.AddToFinalPriorityMap(priorityDict);
            }
        }
        public override void Dispose()
        {
            base.Dispose();
            /*harmonyInstance = null;
            atlasData = null;
            ServerMapInfoData = null;
            ClientMapInfoData = null;
            ServerSeenChunksByAtlases = null;
            ClientSeenChunksByAtlases = null;
            atlasD = null;
            sapi = null;
            capi = null;
            biomeDetector = null;
            biomeTextureMap = null;*/
        }
    }
}
