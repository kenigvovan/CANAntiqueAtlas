using System;
using System.Collections.Generic;
using CANAntiqueAtlas.src;
using CANAntiqueAtlas.src.core;
using CANAntiqueAtlas.src.gui;
using CANAntiqueAtlas.src.gui.render;
using CANAntiqueAtlas.src.harmony;
using CANAntiqueAtlas.src.network.client;
using CANAntiqueAtlas.src.network.server;
using CANAntiqueAtlas.src.playerMovement;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

using Vintagestory.GameContent;
using static CANAntiqueAtlas.src.core.IBiomeDetector;

namespace CANAntiqueAtlas
{
    public class CANAntiqueAtlas : ModSystem
    {
        public static Harmony harmonyInstance;
        public const string harmonyID = "canantiqueatlas.Patches";
        public static AtlasDataHandler atlasData = new AtlasDataHandler();
        public static AtlasData ServerMapInfoData = new("here");
        public static AtlasData ClientMapInfoData = new("here");
        public static Dictionary<int, AtlasSeenData> ServerSeenChunksByAtlases = new();
        public static Dictionary<int, AtlasSeenData> ClientSeenChunksByAtlases = new();
        public static AtlasData atlasD;
        public static Config config;
        public static ICoreServerAPI sapi;
        public static ICoreClientAPI capi;
        internal static IServerNetworkChannel serverChannel;
        internal static IClientNetworkChannel clientChannel;
        public static IBiomeDetector biomeDetector;
        public TextureSetMap textureSetMap;
        public BiomeTextureMap biomeTextureMap;
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
        }
        private void registerDefaultTextureSets(TextureSetMap map)
        {
            map.register(TextureSet.FOREST);
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
                (capi.ModLoader.GetModSystem<CANWorldMapManager>()?.MapLayers[0] as CANChunkMapLayer)?.Event_OnChunkDataReceived(packet.NewMapTiles);
                lock (ClientSeenChunksByAtlases)
                {
                    foreach (var (atlasId, hs) in packet.NewlySeenChunk)
                    {
                        if (ClientSeenChunksByAtlases.TryGetValue(atlasId, out var asd))
                        {
                            foreach (var t in hs)
                            {
                                asd.SetTile(t.X, t.Y, new TileSeen());
                            }
                        }
                        else
                        {
                            var asdNew = new AtlasSeenData(atlasId);
                            foreach (var t in hs)
                            {
                                asdNew.SetTile(t.X, t.Y, new TileSeen());
                            }
                            ClientSeenChunksByAtlases[atlasId] = asdNew;
                        }
                    }
                }
            });
            textureSetMap = TextureSetMap.instance();
            registerDefaultTextureSets(textureSetMap);
            biomeTextureMap = BiomeTextureMap.instance();
            biomeTextureMap.setTexture((int)BiomeType.Rainforest, TextureSet.FOREST);
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
    }
}
