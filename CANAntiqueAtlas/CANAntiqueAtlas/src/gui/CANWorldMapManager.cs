using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using System.Threading;
using CANAntiqueAtlas.src.gui.Map.TileLayer;
using CANAntiqueAtlas.src.gui.Map;
using CANAntiqueAtlas.src.gui.Map.EntityLayer;

namespace CANAntiqueAtlas.src.gui
{
    public class CANWorldMapManager : ModSystem, ICANWorldMapManager
    {
        public Dictionary<string, Type> MapLayerRegistry = new Dictionary<string, Type>();
        public Dictionary<string, double> LayerGroupPositions = new Dictionary<string, double>();

        ICoreAPI api;

        // Client side stuff
        ICoreClientAPI capi;
        IClientNetworkChannel clientChannel;
        public CANGuiDialogWorldMap worldMapDlg;
        public bool IsOpened => worldMapDlg?.IsOpened() == true;


        // Client and Server side stuff
        public List<CANMapLayer> MapLayers = new List<CANMapLayer>();
        Thread mapLayerGenThread;
        public bool IsShuttingDown { get; set; }

        // Server side stuff
        IServerNetworkChannel serverChannel;


        public override bool ShouldLoad(EnumAppSide side)
        {
            return true;
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            RegisterDefaultMapLayers();
            this.api = api;
        }

        public void RegisterDefaultMapLayers()
        {
            RegisterMapLayer<CANChunkMapLayer>("chunks", 0);
            RegisterMapLayer<CANPlayerMapLayer>("players", 0.5);
           /* RegisterMapLayer<EntityMapLayer>("entities", 0.5);
            RegisterMapLayer<WaypointMapLayer>("waypoints", 1);*/
        }

        public void RegisterMapLayer<T>(string code, double position) where T : CANMapLayer
        {
            MapLayerRegistry[code] = typeof(T);
            LayerGroupPositions[code] = position;
        }

        #region Client side

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            capi = api;
            capi.Event.LevelFinalize += OnLvlFinalize;

            capi.Event.RegisterGameTickListener(OnClientTick, 20);

            /*capi.Settings.AddWatcher<bool>("showMinimapHud", (on) => {
                ToggleMap(EnumDialogType.HUD);
            });*/

            capi.Event.LeaveWorld += () =>
            {
                IsShuttingDown = true;
                int i = 0;
                while (mapLayerGenThread != null && mapLayerGenThread.IsAlive && i < 20)
                {
                    Thread.Sleep(50);
                    i++;
                }

                worldMapDlg?.Dispose();

                foreach (var layer in MapLayers)
                {
                    layer?.OnShutDown();
                    layer?.Dispose();
                }
            };

            clientChannel =
                api.Network.RegisterChannel("worldmap")
               .RegisterMessageType(typeof(MapLayerUpdate))
               .RegisterMessageType(typeof(OnViewChangedPacket))
               .RegisterMessageType(typeof(OnMapToggle))
               .SetMessageHandler<MapLayerUpdate>(OnMapLayerDataReceivedClient)
            ;
        }


        private void onWorldMapLinkClicked(LinkTextComponent linkcomp)
        {
            string[] xyzstr = linkcomp.Href.Substring("worldmap://".Length).Split('=');
            int x = xyzstr[1].ToInt();
            int y = xyzstr[2].ToInt();
            int z = xyzstr[3].ToInt();
            string text = xyzstr.Length >= 5 ? xyzstr[4] : "";

            if (worldMapDlg == null || !worldMapDlg.IsOpened() || (worldMapDlg.IsOpened() && worldMapDlg.DialogType == EnumDialogType.HUD))
            {
                ToggleMap(EnumDialogType.Dialog, this.worldMapDlg.atlasId);
            }

            bool exists = false;
            var elem = worldMapDlg.SingleComposer.GetElement("mapElem") as CANGuiElementMap;
            //var wml = elem?.mapLayers.FirstOrDefault(ml => ml is WaypointMapLayer) as WaypointMapLayer;
            Vec3d pos = new Vec3d(x, y, z);
            /*if (wml != null)
            {
                foreach (var wp in wml.ownWaypoints)
                {
                    if (wp.Position.Equals(pos, 0.01))
                    {
                        exists = true;
                        break;
                    }
                }
            }*/

            if (!exists)
            {
                capi.SendChatMessage(string.Format("/waypoint addati {0} ={1} ={2} ={3} {4} {5} {6}", "circle", x, y, z, false, "steelblue", text));
            }

            elem?.CenterMapTo(new BlockPos(x, y, z));
        }

        private void OnClientTick(float dt)
        {
            foreach (CANMapLayer layer in MapLayers)
            {
                layer.OnTick(dt);
            }
        }

        private void OnLvlFinalize()
        {
            if (capi != null && mapAllowedClient())
            {
                /*capi.Input.RegisterHotKey("worldmaphud", Lang.Get("Show/Hide Minimap"), GlKeys.F6, HotkeyType.HelpAndOverlays);
                capi.Input.RegisterHotKey("minimapposition", Lang.Get("keycontrol-minimap-position"), GlKeys.F6, HotkeyType.HelpAndOverlays, false, true, false);*/
                //capi.Input.RegisterHotKey("worldmapdialog", Lang.Get("Show World Map"), GlKeys.M, HotkeyType.HelpAndOverlays);
                //capi.Input.SetHotKeyHandler("worldmaphud", OnHotKeyWorldMapHud);
                //capi.Input.SetHotKeyHandler("minimapposition", OnHotKeyMinimapPosition);
                //capi.Input.SetHotKeyHandler("worldmapdialog", OnHotKeyWorldMapDlg);
                //capi.RegisterLinkProtocol("worldmap", onWorldMapLinkClicked);
            }

            foreach (var val in MapLayerRegistry)
            {
                if (val.Key == "entities" && !api.World.Config.GetAsBool("entityMapLayer")) continue;
                MapLayers.Add((CANMapLayer)Activator.CreateInstance(val.Value, api, this));
            }


            foreach (CANMapLayer layer in MapLayers)
            {
                layer.OnLoaded();
            }

            mapLayerGenThread = new Thread(new ThreadStart(() =>
            {
                while (!IsShuttingDown)
                {
                    foreach (CANMapLayer layer in MapLayers)
                    {
                        layer.OnOffThreadTick(20 / 1000f);
                    }

                    Thread.Sleep(20);
                }
            }));

            mapLayerGenThread.IsBackground = true;
            mapLayerGenThread.Start();

            /*if (capi != null && (capi.Settings.Bool["showMinimapHud"] || !capi.Settings.Bool.Exists("showMinimapHud")) && (worldMapDlg == null || !worldMapDlg.IsOpened()))
            {
                ToggleMap(EnumDialogType.HUD);
            }*/

        }

        private void OnMapLayerDataReceivedClient(MapLayerUpdate msg)
        {
            for (int i = 0; i < msg.Maplayers.Length; i++)
            {
                Type type = MapLayerRegistry[msg.Maplayers[i].ForMapLayer];
                MapLayers.FirstOrDefault(x => x.GetType() == type)?.OnDataFromServer(msg.Maplayers[i].Data);
            }
        }


        public bool mapAllowedClient()
        {
            return capi.World.Config.GetBool("allowMap", true) || capi.World.Player.Privileges.IndexOf("allowMap") != -1;
        }

        private bool OnHotKeyWorldMapHud(KeyCombination comb)
        {
            ToggleMap(EnumDialogType.HUD, this.worldMapDlg.atlasId);
            return true;
        }

        private bool OnHotKeyMinimapPosition(KeyCombination comb)
        {
            int prev = capi.Settings.Int["minimapHudPosition"];
            capi.Settings.Int["minimapHudPosition"] = (prev + 1) % 4;

            if (worldMapDlg == null || !worldMapDlg.IsOpened()) ToggleMap(EnumDialogType.HUD, this.worldMapDlg.atlasId);
            else
            {
                if (worldMapDlg.DialogType == EnumDialogType.HUD)
                {
                    worldMapDlg.Recompose();
                }
            }
            return true;
        }

        public bool OnHotKeyWorldMapDlg(KeyCombination comb, long atlasID)
        {
            ToggleMap(EnumDialogType.Dialog, atlasID);
            return true;
        }


        public void ToggleMap(EnumDialogType asType, long atlasID)
        {
            bool isDlgOpened = worldMapDlg != null && worldMapDlg.IsOpened();

            if (worldMapDlg != null)
            {
                if (!isDlgOpened)
                {
                    //if (asType == EnumDialogType.HUD) capi.Settings.Bool.Set("showMinimapHud", true, false);

                    worldMapDlg.Open(asType);
                    foreach (CANMapLayer layer in MapLayers) 
                    {
                        layer.atlasID = atlasID;
                        layer.OnMapOpenedClient(); 
                    }
                    clientChannel.SendPacket(new OnMapToggle() { OpenOrClose = true });

                    return;
                }
                else
                {
                    worldMapDlg.TryClose();
                    return;

                }
            }

            worldMapDlg = new CANGuiDialogWorldMap(onViewChangedClient, syncViewChange, capi, getTabsOrdered());
            worldMapDlg.OnClosed += () => {
                foreach (CANMapLayer layer in MapLayers) layer.OnMapClosedClient();
                clientChannel.SendPacket(new OnMapToggle() { OpenOrClose = false });

            };

            worldMapDlg.Open(asType);
            worldMapDlg.atlasId = atlasID;
            foreach (CANMapLayer layer in MapLayers)
            {
                layer.OnMapOpenedClient();
                layer.atlasID = atlasID;
            }
            clientChannel.SendPacket(new OnMapToggle() { OpenOrClose = true });
        }

        private List<string> getTabsOrdered()
        {
            Dictionary<string, double> tabs = new Dictionary<string, double>();

            foreach (CANMapLayer layer in MapLayers)
            {
                if (!tabs.ContainsKey(layer.LayerGroupCode))
                {
                    if (!LayerGroupPositions.TryGetValue(layer.LayerGroupCode, out double pos)) pos = 1;
                    tabs[layer.LayerGroupCode] = pos;
                }
            }

            return tabs.OrderBy(val => val.Value).Select(val => val.Key).ToList();
        }

        private void onViewChangedClient(List<FastVec2i> nowVisible, List<FastVec2i> nowHidden)
        {
            foreach (CANMapLayer layer in MapLayers)
            {
                layer.OnViewChangedClient(nowVisible, nowHidden);
            }
        }

        private void syncViewChange(int x1, int z1, int x2, int z2)
        {
            clientChannel.SendPacket(new OnViewChangedPacket() { X1 = x1, Z1 = z1, X2 = x2, Z2 = z2 });
        }

        public void TranslateWorldPosToViewPos(Vec3d worldPos, ref Vec2f viewPos)
        {
            worldMapDlg.TranslateWorldPosToViewPos(worldPos, ref viewPos);
        }

        public void SendMapDataToServer(CANMapLayer forMapLayer, byte[] data)
        {
            if (api.Side == EnumAppSide.Server) return;

            List<MapLayerData> maplayerdatas = new List<MapLayerData>();

            maplayerdatas.Add(new MapLayerData()
            {
                Data = data,
                ForMapLayer = MapLayerRegistry.FirstOrDefault(x => x.Value == forMapLayer.GetType()).Key
            });

            clientChannel.SendPacket(new MapLayerUpdate() { Maplayers = maplayerdatas.ToArray() });
        }
        #endregion

        #region Server Side
        public override void StartServerSide(ICoreServerAPI sapi)
        {
            sapi.Event.ServerRunPhase(EnumServerRunPhase.RunGame, OnLvlFinalize); ;
            sapi.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, () => IsShuttingDown = true);

            serverChannel =
               sapi.Network.RegisterChannel("worldmap")
               .RegisterMessageType(typeof(MapLayerUpdate))
               .RegisterMessageType(typeof(OnViewChangedPacket))
               .RegisterMessageType(typeof(OnMapToggle))
               .SetMessageHandler<OnMapToggle>(OnMapToggledServer)
               .SetMessageHandler<OnViewChangedPacket>(OnViewChangedServer)
               .SetMessageHandler<MapLayerUpdate>(OnMapLayerDataReceivedServer)
            ;

        }

        private void OnMapLayerDataReceivedServer(IServerPlayer fromPlayer, MapLayerUpdate msg)
        {
            for (int i = 0; i < msg.Maplayers.Length; i++)
            {
                Type type = MapLayerRegistry[msg.Maplayers[i].ForMapLayer];
                MapLayers.FirstOrDefault(x => x.GetType() == type)?.OnDataFromClient(msg.Maplayers[i].Data);
            }
        }

        private void OnMapToggledServer(IServerPlayer fromPlayer, OnMapToggle msg)
        {
            foreach (CANMapLayer layer in MapLayers)
            {
                if (layer.DataSide == EnumMapAppSide.Client) continue;

                if (msg.OpenOrClose)
                {
                    layer.OnMapOpenedServer(fromPlayer);
                }
                else
                {
                    layer.OnMapClosedServer(fromPlayer);
                }
            }
        }

        private void OnViewChangedServer(IServerPlayer fromPlayer, OnViewChangedPacket networkMessage)
        {
            foreach (CANMapLayer layer in MapLayers)
            {
                if (layer.DataSide == EnumMapAppSide.Client) continue;

                layer.OnViewChangedServer(fromPlayer, networkMessage.X1, networkMessage.Z1, networkMessage.X2, networkMessage.Z2);
            }
        }

        public void SendMapDataToClient(CANMapLayer forMapLayer, IServerPlayer forPlayer, byte[] data)
        {
            if (api.Side == EnumAppSide.Client) return;
            if (forPlayer.ConnectionState != EnumClientState.Playing) return;

            MapLayerData[] maplayerdatas = new MapLayerData[1] {
                new MapLayerData()
                {
                    Data = data,
                    ForMapLayer = MapLayerRegistry.FirstOrDefault(x => x.Value == forMapLayer.GetType()).Key
                }
            };

            serverChannel.SendPacket(new MapLayerUpdate() { Maplayers = maplayerdatas }, forPlayer);
        }
        #endregion
    }
}
