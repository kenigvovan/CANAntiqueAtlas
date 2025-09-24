using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Claims;
using System.Threading.Channels;
using CANAntiqueAtlas.src;
using CANAntiqueAtlas.src.core;
using CANAntiqueAtlas.src.gui;
using CANAntiqueAtlas.src.harmony;
using CANAntiqueAtlas.src.network.client;
using CANAntiqueAtlas.src.network.server;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.GameContent;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CANAntiqueAtlas
{
    public class CANAntiqueAtlas : ModSystem
    {
        public static Harmony harmonyInstance;
        public const string harmonyID = "canantiqueatlas.Patches";
        public static AtlasDataHandler atlasData = new AtlasDataHandler();
        public static AtlasData atlasD;
        public static Config config;
        public static ICoreServerAPI sapi;
        public static ICoreClientAPI capi;
        internal static IServerNetworkChannel serverChannel;
        internal static IClientNetworkChannel clientChannel;
        public static IBiomeDetector biomeDetector;
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
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            loadConfig(api);
            clientChannel = api.Network.RegisterChannel("canantiqueatlas");
            clientChannel.RegisterMessageType(typeof(BrowsingPositionPacket));
            clientChannel.RegisterMessageType(typeof(MapDataPacket));
            clientChannel.RegisterMessageType(typeof(TileGroupsPacket));
            clientChannel.SetMessageHandler<MapDataPacket>((packet) =>
            {
                if (packet.data == null) return; // Atlas is empty
                AtlasData atlasData = CANAntiqueAtlas.atlasData.GetAtlasData(packet.atlasID, capi.World);
                atlasData = packet.data;
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
