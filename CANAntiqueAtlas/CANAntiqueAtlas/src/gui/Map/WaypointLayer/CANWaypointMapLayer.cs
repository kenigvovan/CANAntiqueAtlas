﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cairo;
using CANAntiqueAtlas.src.item;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.gui.Map.WaypointLayer
{
    [ProtoContract]
    public class CANWaypoint
    {
        [ProtoMember(6)]
        public Vec3d Position = new Vec3d();
        [ProtoMember(10)]
        public string Title;
        [ProtoMember(9)]
        public string Text;
        [ProtoMember(1)]
        public int Color;
        [ProtoMember(2)]
        public string Icon = "circle";
        [ProtoMember(7)]
        public bool ShowInWorld;
        [ProtoMember(5)]
        public bool Pinned;

        [ProtoMember(4)]
        public string OwningPlayerUid = null;
        [ProtoMember(3)]
        public int OwningPlayerGroupId = -1;

        [ProtoMember(8)]
        public bool Temporary;

        [ProtoMember(11)]
        public string Guid { get; set; }
        [ProtoMember(12)]
        public long AtlasId { get; set; }

    }

    public delegate LoadedTexture CreateIconTextureDelegate();


    public class CANWaypointMapLayer : CANMarkerMapLayer
    {
        // Server side
        public Dictionary<long, HashSet<CANWaypoint>>Waypoints = new();
        ICoreServerAPI sapi;

        // Client side
        public Dictionary<long, HashSet<CANWaypoint>> ownWaypoints = new();
        List<CANMapComponent> wayPointComponents = new();
        public MeshRef quadModel;

        List<CANMapComponent> tmpWayPointComponents = new();

        public Dictionary<string, LoadedTexture> texturesByIcon;

        public override bool RequireChunkLoaded => false;


        /// <summary>
        /// List
        /// </summary>
        public OrderedDictionary<string, CreateIconTextureDelegate> WaypointIcons { get; set; } = new();

        static string[] hexcolors = new string[] {
            "#F9D0DC", "#F179AF", "#F15A4A", "#ED272A", "#A30A35", "#FFDE98", "#EFFD5F", "#F6EA5E", "#FDBB3A", "#C8772E", "#F47832",
            "C3D941", "#9FAB3A", "#94C948", "#47B749", "#366E4F", "#516D66", "93D7E3", "#7698CF", "#20909E", "#14A4DD", "#204EA2",
            "#28417A", "#C395C4", "#92479B", "#8E007E", "#5E3896", "D9D4CE", "#AFAAA8", "#706D64", "#4F4C2B", "#BF9C86", "#9885530", "#5D3D21", "#FFFFFF", "#080504"
        };

        public List<int> WaypointColors { get; set; } = new List<int>()
        {

        };

        public CANWaypointMapLayer(ICoreAPI api, ICANWorldMapManager mapSink) : base(api, mapSink)
        {
            WaypointColors = new List<int>();
            for (int i = 0; i < hexcolors.Length; i++)
            {
                WaypointColors.Add(ColorUtil.Hex2Int(hexcolors[i]));
            }

            var icons = api.Assets.GetMany("textures/icons/worldmap/", null, false);
            var capi = api as ICoreClientAPI;
            foreach (var icon in icons)
            {
                string name = icon.Name.Substring(0, icon.Name.IndexOf('.'));

                name = Regex.Replace(name, @"\d+\-", "");

                if (api.Side == EnumAppSide.Server)
                {
                    WaypointIcons[name] = () => null;
                }
                else
                {
                    WaypointIcons[name] = () =>
                    {
                        var size = (int)Math.Ceiling(20 * RuntimeEnv.GUIScale);
                        return capi.Gui.LoadSvg(icon.Location, size, size, size, size, ColorUtil.WhiteArgb);
                    };

                    capi.Gui.Icons.CustomIcons["wp" + name.UcFirst()] = (ctx, x, y, w, h, rgba) =>
                    {
                        var col = ColorUtil.ColorFromRgba(rgba);

                        capi.Gui.DrawSvg(icon, ctx.GetTarget() as ImageSurface, ctx.Matrix, x, y, (int)w, (int)h, col);
                    };
                }
            }

            if (api.Side == EnumAppSide.Server)
            {
                ICoreServerAPI sapi = api as ICoreServerAPI;
                this.sapi = sapi;

                sapi.Event.GameWorldSave += OnSaveGameGettingSaved;
                sapi.Event.PlayerDeath += Event_PlayerDeath;
                var parsers = sapi.ChatCommands.Parsers;
                sapi.ChatCommands.Create("canwaypoint")
                    .WithDescription("Put a waypoint at this location which will be visible for you on the map")
                    .RequiresPrivilege(Privilege.chat)
                    .BeginSubCommand("deathwp")
                        .WithDescription("Enable/Disable automatic adding of a death waypoint")
                        .WithArgs(parsers.OptionalBool("enabled"))
                        .RequiresPlayer()
                        .HandleWith(OnCmdWayPointDeathWp)
                    .EndSubCommand()

                    .BeginSubCommand("add")
                        .WithDescription("Add a waypoint to the map")
                        .RequiresPlayer()
                        .WithArgs(parsers.Color("color"), parsers.All("title"))
                        .HandleWith(OnCmdWayPointAdd)
                    .EndSubCommand()

                    .BeginSubCommand("addp")
                        .RequiresPlayer()
                        .WithDescription("Add a waypoint to the map")
                        .WithArgs(parsers.Color("color"), parsers.All("title"))
                        .HandleWith(OnCmdWayPointAddp)
                    .EndSubCommand()

                    .BeginSubCommand("addat")
                        .WithDescription("Add a waypoint to the map")
                        .RequiresPlayer()
                        .WithArgs(parsers.WorldPosition("position"), parsers.Bool("pinned"), parsers.Color("color"), parsers.All("title"))
                        .HandleWith(OnCmdWayPointAddat)
                    .EndSubCommand()

                    .BeginSubCommand("addati")
                        .WithDescription("Add a waypoint to the map")
                        .RequiresPlayer()
                        .WithArgs(parsers.Word("icon"), parsers.WorldPosition("position"), parsers.Bool("pinned"), parsers.Color("color"), parsers.All("title"))
                        .HandleWith(OnCmdWayPointAddati)
                    .EndSubCommand()

                   /* .BeginSubCommand("modify")
                        .WithDescription("")
                        .RequiresPlayer()
                        .WithArgs(parsers.Int("waypoint_id"), parsers.Color("color"), parsers.Word("icon"), parsers.Bool("pinned"), parsers.All("title"))
                        .HandleWith(OnCmdWayPointModify)
                    .EndSubCommand()*/

                    .BeginSubCommand("remove")
                        .WithDescription("Remove a waypoint by its id. Get a lost of ids using /waypoint list")
                        .RequiresPlayer()
                        .WithArgs(parsers.Word("waypoint_guid"))
                        .HandleWith(OnCmdWayPointRemove)
                    .EndSubCommand()

                    /*.BeginSubCommand("list")
                        .WithDescription("List your own waypoints")
                        .RequiresPlayer()
                        .WithArgs(parsers.OptionalWordRange("details", "details", "d"))
                        .HandleWith(OnCmdWayPointList)
                    .EndSubCommand()*/
                    ;

                /*sapi.ChatCommands.Create("tpwp")
                    .WithDescription("Teleport yourself to a waypoint starting with the supplied name")
                    .RequiresPrivilege(Privilege.tp)
                    .WithArgs(parsers.All("name"))
                    .HandleWith(OnCmdTpTo)*/
                    ;
            }
            else
            {
                quadModel = (api as ICoreClientAPI).Render.UploadMesh(QuadMeshUtil.GetQuad());
            }
        }

        /*private TextCommandResult OnCmdWayPointList(TextCommandCallingArgs args)
        {
            if (IsMapDisallowed(out var textCommandResult)) return textCommandResult;
            var detailed = args[0] as string == "details" || args[0] as string == "d";
            var wps = new StringBuilder();
            var i = 0;
            foreach (var p in Waypoints.Where((p) => p.OwningPlayerUid == args.Caller.Player.PlayerUID).ToArray())
            {
                var pos = p.Position.Clone();
                pos.X -= api.World.DefaultSpawnPosition.X;
                pos.Z -= api.World.DefaultSpawnPosition.Z;
                if (detailed)
                {
                    wps.AppendLine(string.Format("{0}: {1} at {2} {3} {4}", i, p.Title, pos.AsBlockPos, ColorUtil.Int2Hex(p.Color), p.Icon));
                }
                else
                {
                    wps.AppendLine(string.Format("{0}: {1} at {2}", i, p.Title, pos.AsBlockPos));
                }
                i++;
            }

            if (wps.Length == 0)
            {
                return TextCommandResult.Success(Lang.Get("You have no waypoints"));
            }

            return TextCommandResult.Success(Lang.Get("Your waypoints:") + "\n" + wps.ToString());
        }*/

        private bool IsMapDisallowed(out TextCommandResult response)
        {
            /*if (!api.World.Config.GetBool("allowMap", true))
            {
                response = TextCommandResult.Success(Lang.Get("Maps are disabled on this server"));
                return true;
            }*/

            response = null;
            return false;
        }

        private TextCommandResult OnCmdWayPointRemove(TextCommandCallingArgs args)
        {
            if (IsMapDisallowed(out var textCommandResult)) return textCommandResult;

            var player = args.Caller.Player as IServerPlayer;
            //var id = (int)args.Parsers[0].GetValue();
            var atlasItemstack = player.InventoryManager.ActiveHotbarSlot?.Itemstack;
            long atlasId = -1;
            if (atlasItemstack.Collectible is CANItemAtlas && atlasItemstack.Attributes.HasAttribute("atlasID"))
            {
                atlasId = atlasItemstack.Attributes.GetLong("atlasID");
            }
            else
            {
                return TextCommandResult.Error("No atlas");
            }

            //CANWaypoint[] ownwpaypoints = Waypoints.Where((p) => p.OwningPlayerUid == player.PlayerUID).ToArray();

            /*if (ownwpaypoints.Length == 0)
            {
                return TextCommandResult.Success(Lang.Get("You have no waypoints to delete"));
            }*/
            if (atlasId < 0)
            {
                return TextCommandResult.Error("No atlas");
            }

            //if (args.Parsers[0].IsMissing || id < 0 /*|| id >= ownwpaypoints.Length*/)
            {
                //return TextCommandResult.Success(Lang.Get("Invalid waypoint number, valid ones are 0..{0}", ownwpaypoints.Length - 1));
            }

            //Waypoints.Remove(args.Parsers[0].ToString());
            var c = Waypoints[atlasId].FirstOrDefault(wp => (wp.Guid?.ToString() ?? "") == args.Parsers[0].GetValue().ToString());
            Waypoints[atlasId].Remove(c);
            RebuildMapComponents();
            foreach (var pl in CANAntiqueAtlas.sapi.ModLoader.GetModSystem<CANAntiqueAtlas>().pmls.GetPlayersWithAtlas(atlasId)) 
            { 
                ResendWaypoints(player); 
            }
            return TextCommandResult.Success(Lang.Get("Ok, deleted waypoint."));
        }

        private TextCommandResult OnCmdWayPointDeathWp(TextCommandCallingArgs args)
        {
            if (IsMapDisallowed(out var textCommandResult)) return textCommandResult;

            if (!api.World.Config.GetBool("allowDeathwaypointing", true))
            {
                return TextCommandResult.Success(Lang.Get("Death waypointing is disabled on this server"));
            }

            var player = args.Caller.Player as IServerPlayer;
            if (args.Parsers[0].IsMissing)
            {
                bool on = player.GetModData<bool>("deathWaypointing");
                return TextCommandResult.Success(Lang.Get("Death waypoint is {0}", on ? Lang.Get("on") : Lang.Get("off")));
            }
            else
            {
                bool on = (bool)args.Parsers[0].GetValue();
                player.SetModData("deathWaypointing", on);
                return TextCommandResult.Success(Lang.Get("Death waypoint now {0}", on ? Lang.Get("on") : Lang.Get("off")));
            }
        }

        private void Event_PlayerDeath(IServerPlayer byPlayer, DamageSource damageSource)
        {
            if (!api.World.Config.GetBool("allowMap", true) || !api.World.Config.GetBool("allowDeathwaypointing", true) || !byPlayer.GetModData("deathWaypointing", true)) return;

            string title = Lang.Get("You died here");
            /*for (int i = 0; i < Waypoints.Count; i++)
            {
                var wp = Waypoints[i];
                if (wp.OwningPlayerUid == byPlayer.PlayerUID && wp.Title == title)
                {
                    Waypoints.RemoveAt(i);
                    i--;
                }
            }*/
            var ids = CANAntiqueAtlas.sapi.ModLoader.GetModSystem<CANAntiqueAtlas>().pmls.GetPlayerAtlasIds(byPlayer);
            foreach (var it in ids)
            {
                CANWaypoint waypoint = new CANWaypoint()
                {
                    Color = ColorUtil.ColorFromRgba(200, 200, 200, 255),
                    OwningPlayerUid = byPlayer.PlayerUID,
                    Position = byPlayer.Entity.Pos.XYZ,
                    Title = title,
                    Icon = "gravestone",
                    Pinned = true,
                    AtlasId = it
                };

                AddWaypoint(waypoint, byPlayer);
            }
        }

        /*private TextCommandResult OnCmdTpTo(TextCommandCallingArgs args)
        {
            var player = args.Caller.Player;
            var name = (args.Parsers[0].GetValue() as string).ToLowerInvariant();
            var playersWaypoints = Waypoints.Where((p) => p.OwningPlayerUid == player.PlayerUID).ToArray();

            foreach (var wp in playersWaypoints)
            {
                if (wp.Title != null && wp.Title.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    player.Entity.TeleportTo(wp.Position);
                    return TextCommandResult.Success(Lang.Get("Ok teleported you to waypoint {0}.", wp.Title));
                }
            }
            return TextCommandResult.Success(Lang.Get("No such waypoint found"));
        }*/

        private TextCommandResult OnCmdWayPointAdd(TextCommandCallingArgs args)
        {
            if (IsMapDisallowed(out var textCommandResult)) return textCommandResult;

            var parsedColor = (System.Drawing.Color)args.Parsers[0].GetValue();
            var title = args.Parsers[1].GetValue() as string;
            var player = args.Caller.Player as IServerPlayer;
            return AddWp(player, player.Entity.Pos.XYZ, title, parsedColor, "circle", false);
        }

        private TextCommandResult OnCmdWayPointAddp(TextCommandCallingArgs args)
        {
            if (IsMapDisallowed(out var textCommandResult)) return textCommandResult;

            var parsedColor = (System.Drawing.Color)args.Parsers[0].GetValue();
            var title = args.Parsers[1].GetValue() as string;
            var player = args.Caller.Player as IServerPlayer;
            return AddWp(player, player.Entity.Pos.XYZ, title, parsedColor, "circle", true);
        }

        private TextCommandResult OnCmdWayPointAddat(TextCommandCallingArgs args)
        {
            if (IsMapDisallowed(out var textCommandResult)) return textCommandResult;

            var pos = args.Parsers[0].GetValue() as Vec3d;
            var pinned = (bool)args.Parsers[1].GetValue();
            var parsedColor = (System.Drawing.Color)args.Parsers[2].GetValue();
            var title = args.Parsers[3].GetValue() as string;


            var player = args.Caller.Player as IServerPlayer;
            return AddWp(player, pos, title, parsedColor, "circle", pinned);
        }

        private TextCommandResult OnCmdWayPointAddati(TextCommandCallingArgs args)
        {
            if (IsMapDisallowed(out var textCommandResult)) return textCommandResult;

            var icon = args.Parsers[0].GetValue() as string;
            var pos = args.Parsers[1].GetValue() as Vec3d;
            var pinned = (bool)args.Parsers[2].GetValue();
            var parsedColor = (System.Drawing.Color)args.Parsers[3].GetValue();
            var title = args.Parsers[4].GetValue() as string;

            var player = args.Caller.Player as IServerPlayer;
            return AddWp(player, pos, title, parsedColor, icon, pinned);
        }

        /*private TextCommandResult OnCmdWayPointModify(TextCommandCallingArgs args)
        {
            if (IsMapDisallowed(out var textCommandResult)) return textCommandResult;

            var wpIndex = (int)args.Parsers[0].GetValue();

            var parsedColor = (System.Drawing.Color)args.Parsers[1].GetValue();
            var icon = args.Parsers[2].GetValue() as string;
            var pinned = (bool)args.Parsers[3].GetValue();
            var title = args.Parsers[4].GetValue() as string;

            var player = args.Caller.Player as IServerPlayer;

            var playerWaypoints = Waypoints.Where(p => p.OwningPlayerUid == player.PlayerUID).ToArray();

            if (args.Parsers[0].IsMissing || wpIndex < 0 || wpIndex >= playerWaypoints.Length)
            {
                return TextCommandResult.Success(Lang.Get("command-modwaypoint-invalidindex", playerWaypoints.Length - 1));
            }

            if (string.IsNullOrEmpty(title))
            {
                return TextCommandResult.Success(Lang.Get("command-waypoint-notext"));
            }

            playerWaypoints[wpIndex].Color = parsedColor.ToArgb() | (255 << 24);
            playerWaypoints[wpIndex].Title = title;
            playerWaypoints[wpIndex].Pinned = pinned;

            if (icon != null)
            {
                playerWaypoints[wpIndex].Icon = icon;
            }

            ResendWaypoints(player);
            return TextCommandResult.Success(Lang.Get("Ok, waypoint nr. {0} modified", wpIndex));
        }*/

        private TextCommandResult AddWp(IServerPlayer player, Vec3d pos, string title, System.Drawing.Color parsedColor, string icon, bool pinned)
        {
            if (string.IsNullOrEmpty(title))
            {
                return TextCommandResult.Success(Lang.Get("command-waypoint-notext"));
            }
            List<int> returnIds = new();
            var atlasItemstack = player.InventoryManager.ActiveHotbarSlot?.Itemstack;
            long atlasId = -1;
            if (atlasItemstack.Collectible is CANItemAtlas && atlasItemstack.Attributes.HasAttribute("atlasID"))
            {
                atlasId = atlasItemstack.Attributes.GetLong("atlasID");
            }
            else
            {
                return TextCommandResult.Error("No atlas");
            }
            var ids = CANAntiqueAtlas.sapi.ModLoader.GetModSystem<CANAntiqueAtlas>().pmls.GetPlayerAtlasIds(player);
            //foreach (var it in ids)
            {
                var waypoint = new CANWaypoint()
                {
                    Color = parsedColor.ToArgb() | (255 << 24),
                    OwningPlayerUid = player.PlayerUID,
                    Position = pos,
                    Title = title,
                    Icon = icon,
                    Pinned = pinned,
                    Guid = Guid.NewGuid().ToString(),
                    AtlasId = atlasId
                };

                AddWaypoint(waypoint, player);
            }        
            return TextCommandResult.Success(Lang.Get("Ok, waypoint nr. {0} added", returnIds.ToArray()));
        }

        public int AddWaypoint(CANWaypoint waypoint, IServerPlayer player)
        {
            if(Waypoints.TryGetValue(waypoint.AtlasId, out var li))
            {
                li.Add(waypoint);
            }else
            {
                HashSet<CANWaypoint> newLi = new (){ waypoint };
                Waypoints[waypoint.AtlasId] = newLi;
            }
            //Waypoints.Add(waypoint);

            //CANWaypoint[] ownwpaypoints = Waypoints.Where((p) => p.OwningPlayerUid == player.PlayerUID).ToArray();

            ResendWaypoints(player);

            return Waypoints[waypoint.AtlasId].Count();
        }

        private void OnSaveGameGettingSaved()
        {
            sapi.WorldManager.SaveGame.StoreData("CAN_playerMapMarkers_v2", SerializerUtil.Serialize(Waypoints));
        }


        [Obsolete("Receiving the OnViewChangedPacket now calls: OnViewChangedServer(fromPlayer, int x1, int z1, int x2, int z2) but retained in 1.20.10 for backwards compatibility")]
        public override void OnViewChangedServer(IServerPlayer fromPlayer, List<FastVec2i> nowVisible, List<FastVec2i> nowHidden)
        {
            ResendWaypoints(fromPlayer);
        }

        public override void OnViewChangedServer(IServerPlayer fromPlayer, int x1, int z1, int x2, int z2)
        {
            OnViewChangedServer(fromPlayer, null, null);
        }


        public override void OnMapOpenedClient()
        {
            reloadIconTextures();

            ensureIconTexturesLoaded();

            RebuildMapComponents();
        }

        public void reloadIconTextures()
        {
            if (texturesByIcon != null)
            {
                foreach (var val in texturesByIcon)
                {
                    val.Value.Dispose();
                }
            }

            texturesByIcon = null;
            ensureIconTexturesLoaded();
        }

        protected void ensureIconTexturesLoaded()
        {
            if (texturesByIcon != null) return;

            texturesByIcon = new Dictionary<string, LoadedTexture>();

            foreach (var val in WaypointIcons)
            {
                texturesByIcon[val.Key] = val.Value();
            }
        }


        public override void OnMapClosedClient()
        {
            foreach (var val in tmpWayPointComponents)
            {
                wayPointComponents.Remove(val);
            }

            tmpWayPointComponents.Clear();
        }

        public override void Dispose()
        {
            if (texturesByIcon != null)
            {
                foreach (var val in texturesByIcon)
                {
                    val.Value.Dispose();
                }
            }
            texturesByIcon = null;
            quadModel?.Dispose();

            base.Dispose();
        }

        public override void OnLoaded()
        {
            if (sapi != null)
            {
                try
                {
                    byte[] data = sapi.WorldManager.SaveGame.GetData("CAN_playerMapMarkers_v2");
                    if (data != null)
                    {
                        Waypoints = SerializerUtil.Deserialize<Dictionary<long, HashSet<CANWaypoint>>>(data);
                        sapi.World.Logger.Notification("Successfully loaded " + Waypoints.Count + " waypoints");
                    }
                    else
                    {
                        //data = sapi.WorldManager.SaveGame.GetData("playerMapMarkers");
                        if (data != null) Waypoints = JsonUtil.FromBytes<Dictionary<long, HashSet<CANWaypoint>>>(data);
                    }

                    /*for (int i = 0; i < Waypoints.Count; i++)
                    {
                        var wp = Waypoints[i];
                        if (wp == null)
                        {
                            //TODO
                            sapi.World.Logger.Error("Waypoint with no information loaded, will remove");
                            //Waypoints.RemoveAt(i);
                            i--;
                        }
                        if (wp.Title == null) wp.Title = wp.Text; // Not sure how this happens. For some reason the title moved into text
                    }*/
                    List<long> removeKeys = new();
                    foreach (var it in this.Waypoints)
                    {
                        if(it.Value == null)
                        {
                            removeKeys.Add(it.Key);
                            sapi.World.Logger.Error("Waypoint with no information loaded, will remove");
                            continue;
                        }
                    }
                    foreach (var it in removeKeys)
                    {
                        this.Waypoints.Remove(it);
                    }
                }
                catch (Exception e)
                {
                    sapi.World.Logger.Error("Failed deserializing player map markers. Won't load them, sorry! Exception thrown:");
                    sapi.World.Logger.Error(e);
                }
                foreach(var ih in this.Waypoints)
                {
                    foreach (var wp in ih.Value)
                    {
                        if (wp == null) continue;
                        if (wp.Title == null) wp.Title = wp.Text; // Not sure how this happens. For some reason the title moved into text
                        if (wp.Guid == null) wp.Guid = Guid.NewGuid().ToString();
                    }
                }
            }

        }

        public override void OnDataFromServer(byte[] data)
        {
            //ownWaypoints.Clear();
            var di = SerializerUtil.Deserialize<Dictionary<long, HashSet<CANWaypoint>>>(data);
            List<long> toRemove = new();
            foreach(var it in di)
            {
                if(it.Value == null)
                {
                    toRemove.Add(it.Key);
                    continue;
                }
                ownWaypoints[it.Key] = it.Value;
                /*if (ownWaypoints.TryGetValue(it.Key, out var localVal))
                {
                    ownWaypoints[it.Key] = it.Value;
                }*/
            }
            foreach(var it in toRemove)
            {
                ownWaypoints.Remove(it);
            }
            //ownWaypoints.AddRange();
            RebuildMapComponents();
        }


        public void AddTemporaryWaypoint(CANWaypoint waypoint)
        {
            CANWaypointMapComponent comp = new CANWaypointMapComponent(ownWaypoints.Count, waypoint, this, api as ICoreClientAPI);
            wayPointComponents.Add(comp);
            tmpWayPointComponents.Add(comp);
        }


        private void RebuildMapComponents()
        {
            if (!mapSink.IsOpened) return;

            foreach (var val in tmpWayPointComponents)
            {
                wayPointComponents.Remove(val);
            }

            foreach (CANWaypointMapComponent comp in wayPointComponents)
            {
                comp.Dispose();
            }

            wayPointComponents.Clear();
            if(this.ownWaypoints.TryGetValue(this.atlasID, out var wps))
            {
                int i = 0;
                foreach (var it in wps)
                {
                    CANWaypointMapComponent comp = new CANWaypointMapComponent(i, it, this, api as ICoreClientAPI);
                    i++;
                    wayPointComponents.Add(comp);
                }
            }
           /* foreach (var atlasHashSet in this.ownWaypoints)
            {
                int i = 0;
                foreach(var it in atlasHashSet.Value)
                {
                    it.AtlasId = atlasHashSet.Key;
                    CANWaypointMapComponent comp = new CANWaypointMapComponent(i, it, this, api as ICoreClientAPI);
                    i++;
                    wayPointComponents.Add(comp);
                }
            }*/

            wayPointComponents.AddRange(tmpWayPointComponents);
        }


        public override void Render(CANGuiElementMap mapElem, float dt)
        {
            if (!Active) return;

            foreach (var val in wayPointComponents)
            {
                val.Render(mapElem, dt);
            }
        }

        public override void OnMouseMoveClient(MouseEvent args, CANGuiElementMap mapElem, StringBuilder hoverText)
        {
            if (!Active) return;

            foreach (var val in wayPointComponents)
            {
                val.OnMouseMove(args, mapElem, hoverText);
            }
        }

        public override void OnMouseUpClient(MouseEvent args, CANGuiElementMap mapElem)
        {
            if (!Active) return;

            foreach (var val in wayPointComponents)
            {
                val.OnMouseUpOnElement(args, mapElem);
                if (args.Handled) break;
            }
        }


        void ResendWaypoints(IServerPlayer toPlayer)
        {
            Dictionary<int, PlayerGroupMembership> memberOfGroups = toPlayer.ServerData.PlayerGroupMemberships;
            Dictionary<long, HashSet<CANWaypoint>> hisMarkers = new();
            HashSet<long> atlasIds = CANAntiqueAtlas.sapi.ModLoader.GetModSystem<CANAntiqueAtlas>().pmls.GetPlayerAtlasIds(toPlayer);

            foreach(var kvp in atlasIds)
            {
                if(this.Waypoints.TryGetValue(kvp, out var wp))
                {
                    hisMarkers[kvp] = wp;
                }
            }
            mapSink.SendMapDataToClient(this, toPlayer, SerializerUtil.Serialize(hisMarkers));
            /*foreach (CANWaypoint marker in Waypoints)
            {
                if (toPlayer.PlayerUID != marker.OwningPlayerUid && !memberOfGroups.ContainsKey(marker.OwningPlayerGroupId)) continue;
                hisMarkers.Add(marker);
            }

            mapSink.SendMapDataToClient(this, toPlayer, SerializerUtil.Serialize(hisMarkers));*/
        }



        public override string Title => "Player Set Markers";
        public override EnumMapAppSide DataSide => EnumMapAppSide.Server;

        public override string LayerGroupCode => "waypoints";
    }
}
