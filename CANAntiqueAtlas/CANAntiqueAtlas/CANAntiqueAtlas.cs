using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using CANAntiqueAtlas.src;
using CANAntiqueAtlas.src.core;
using CANAntiqueAtlas.src.core.BiomeDetectors;
using CANAntiqueAtlas.src.gui;
using CANAntiqueAtlas.src.gui.Map.TileLayer;
using CANAntiqueAtlas.src.gui.render;
using CANAntiqueAtlas.src.harmony;
using CANAntiqueAtlas.src.network.client;
using CANAntiqueAtlas.src.network.server;
using CANAntiqueAtlas.src.playerMovement;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
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
        public static AtlasData atlasD;
        public static Config config;
        public static ICoreServerAPI sapi;
        public static ICoreClientAPI capi;
        internal static IServerNetworkChannel serverChannel;
        internal static IClientNetworkChannel clientChannel;
        public static IBiomeDetector biomeDetector;
        public TextureSetMap textureSetMap;
        public static BiomeTextureMap biomeTextureMap;
        PlayerMovementsListnerServer pmls;
        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass("CANItemAtlas", typeof(CANItemAtlas));
            api.RegisterItemClass("CANItemEmptyAtlas", typeof(CANItemEmptyAtlas));
            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(typeof(WorldMapManager).GetMethod("ShouldLoad", new Type[] { typeof(EnumAppSide) }), prefix: new HarmonyMethod(typeof(harmPatches).GetMethod("Prefix_ModSystemShouldLoad")));
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            loadConfig(api);
            serverChannel = sapi.Network.RegisterChannel("canantiqueatlas");
            serverChannel.RegisterMessageType(typeof(BrowsingPositionPacket));
            serverChannel.RegisterMessageType(typeof(MapDataPacket));
            serverChannel.RegisterMessageType(typeof(TileGroupsPacket));
            serverChannel.RegisterMessageType(typeof(NewlySeenChunksAndMapData));
            serverChannel.RegisterMessageType(typeof(PlayerJoinedMapData));
            serverChannel.RegisterMessageType(typeof(PlayerJoinedMapDataSeen));
            serverChannel.SetMessageHandler<BrowsingPositionPacket>((player, packet) =>
            {
                ItemStack atlasStack = new ItemStack(player.Entity.World.GetItem(new AssetLocation("canantiqueatlas:canatlas")), 1);
                atlasStack.Attributes.SetInt("atlasID", packet.atlasID);
                // Make sure it's this player's atlas :^)
                if (!player.InventoryManager.Find(sl => sl?.Itemstack.Equals(atlasStack) ?? false))
                {
                    /*Log.warn("Player %s attempted to put marker into someone else's Atlas #%d",
                            player.getGameProfile().getName(), atlasID);*/
                    return;
                }
                atlasData.GetAtlasData(packet.atlasID, player.Entity.World)
                    .GetDimensionData().setBrowsingPosition(packet.x, packet.y, packet.zoom);
            });
            biomeDetector = new BiomeDetectorBase();
            pmls = new PlayerMovementsListnerServer();
            sapi.Event.RegisterGameTickListener(pmls.CheckPlayerMove, 2000);
            sapi.Event.PlayerNowPlaying += pmls.OnPlayerNowPlaying;

            Dictionary<AssetLocation, JToken> many = api.Assets.GetMany<JToken>(api.Server.Logger, "config/conditions/default_bioms");

            foreach (KeyValuePair<AssetLocation, JToken> val in many)
            {
                if (val.Value is JObject)
                {
                    BiomeConditions conditions = val.Value.ToObject<BiomeConditions>();
                    biomeDetector.RegisterBiome(conditions);
                }
                if (val.Value is JArray)
                {
                    foreach (JToken token in (val.Value as JArray))
                    {
                        BiomeConditions conditions = token.ToObject<BiomeConditions>();
                        biomeDetector.RegisterBiome(conditions);
                    }
                }
            }
            api.ChatCommands.Create("canatlas")
               .RequiresPlayer()
               .RequiresPrivilege(Privilege.controlserver)
               .BeginSub("shst")
                   .HandleWith(ShowStatsCommand)
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
        private void registerDefaultTextureSets(TextureSetMap map)
        {
            map.register(TextureSet.TEMPERATEFOREST);
            map.register(TextureSet.SNOW);
            map.register(TextureSet.TEST);
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            loadConfig(api);
            clientChannel = api.Network.RegisterChannel("canantiqueatlas");
            clientChannel.RegisterMessageType(typeof(BrowsingPositionPacket));
            clientChannel.RegisterMessageType(typeof(MapDataPacket));
            clientChannel.RegisterMessageType(typeof(TileGroupsPacket));
            clientChannel.RegisterMessageType(typeof(NewlySeenChunksAndMapData));
            clientChannel.RegisterMessageType(typeof(PlayerJoinedMapData));
            clientChannel.RegisterMessageType(typeof(PlayerJoinedMapDataSeen));
            clientChannel.SetMessageHandler<MapDataPacket>((packet) =>
            {
                if (packet.data == null) return; // Atlas is empty
                //AtlasData atlasData = CANAntiqueAtlas.atlasData.GetAtlasData(packet.atlasID, capi.World);
                //atlasData = packet.data;
                //CANAntiqueAtlas.clientAtlasData = packet.data;
                // GuiAtlas may already be opened at (0, 0) browsing position, force load saved position:
                /*if (CANAntiqueAtlas.config.doSaveBrowsingPos &&
                        Minecraft.getMinecraft().currentScreen is GuiAtlas) {
                    ((GuiAtlas)Minecraft.getMinecraft().currentScreen).loadSavedBrowsingPosition();*/
            });
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
            textureSetMap = TextureSetMap.instance();
            registerDefaultTextureSets(textureSetMap);
            biomeTextureMap = BiomeTextureMap.instance();
            //biomeTextureMap.setTexture((int)BiomeType.Rainforest, TextureSet.JUNGLE);
            biomeTextureMap.setTexture((int)BiomeType.Glacier, TextureSet.SNOW);
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
            biomeTextureMap.setTexture((int)BiomeType.Jungle, TextureSet.JUNGLE);
            // biomeTextureMap.setTexture((int)BiomeType.Taiga, TextureSet.SNOW_PINES);
        }
        private void loadConfig(ICoreAPI api)
        {        
            config = api.LoadModConfig<Config>(this.Mod.Info.ModID + ".json");
               
            if (config == null)
            {
                config = new Config();
            }
               
            api.StoreModConfig<Config>(config, this.Mod.Info.ModID + ".json");            
            return;          
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
