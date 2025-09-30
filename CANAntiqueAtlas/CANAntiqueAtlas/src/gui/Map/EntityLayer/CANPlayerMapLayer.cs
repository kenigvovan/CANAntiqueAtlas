using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.gui.Map.EntityLayer
{
    public class CANPlayerMapLayer: CANMarkerMapLayer
    {
        Dictionary<IPlayer, CANEntityMapComponent> MapComps = new Dictionary<IPlayer, CANEntityMapComponent>();
        ICoreClientAPI capi;
        LoadedTexture ownTexture;
        LoadedTexture otherTexture;

        public override string Title => "Players";
        public override EnumMapAppSide DataSide => EnumMapAppSide.Client;

        public override string LayerGroupCode => "terrain";

        public CANPlayerMapLayer(ICoreAPI api, ICANWorldMapManager mapsink) : base(api, mapsink)
        {
            capi = (api as ICoreClientAPI);
        }

        private void Event_PlayerDespawn(IClientPlayer byPlayer)
        {
            if (MapComps.TryGetValue(byPlayer, out CANEntityMapComponent mp))
            {
                mp.Dispose();
                MapComps.Remove(byPlayer);
            }
        }

        private void Event_PlayerSpawn(IClientPlayer byPlayer)
        {
            if (capi.World.Config.GetBool("mapHideOtherPlayers", false) && byPlayer.PlayerUID != capi.World.Player.PlayerUID)
            {
                return;
            }

            if (mapSink.IsOpened && !MapComps.ContainsKey(byPlayer))
            {
                CANEntityMapComponent cmp = new CANEntityMapComponent(capi, otherTexture, byPlayer.Entity);
                MapComps[byPlayer] = cmp;
            }
        }

        public override void OnLoaded()
        {
            if (capi != null)
            {
                // Only client side
                capi.Event.PlayerEntitySpawn += Event_PlayerSpawn;
                capi.Event.PlayerEntityDespawn += Event_PlayerDespawn;
            }
        }


        public override void OnMapOpenedClient()
        {
            int size = (int)GuiElement.scaled(32);

            if (ownTexture == null)
            {
                //ImageSurface surface = new ImageSurface(Format.Argb32, size, size);
               // Context ctx = new Context(surface);
                //ctx.SetSourceRGBA(0, 0, 0, 0);
                //ctx.Paint();
                //capi.Gui.Icons.DrawMapPlayer(ctx, 0, 0, size, size, new double[] { 0, 0, 0, 1 }, new double[] { 1, 1, 1, 1 });

                //ownTexture = new LoadedTexture(capi, capi.Gui.LoadCairoTexture(surface, false), size / 2, size / 2);
                //this.capi.Render.GetOrLoadTexture("canantiqueatlas:gui/player.png", ref ownTexture);
                var socketSurface = GuiElement.getImageSurfaceFromAsset(this.capi, capi.Assets.TryGet("canantiqueatlas:textures/gui/player.png").Location, 255);
                
                ownTexture = new LoadedTexture(capi, capi.Gui.LoadCairoTexture(socketSurface, false), size / 2, size / 2);
                socketSurface.Dispose();
                // ctx.Dispose();
                // surface.Dispose();
            }

            if (otherTexture == null)
            {
                ImageSurface surface = new ImageSurface(Format.Argb32, size, size);
                Context ctx = new Context(surface);
                ctx.SetSourceRGBA(0, 0, 0, 0);
                ctx.Paint();
                capi.Gui.Icons.DrawMapPlayer(ctx, 0, 0, size, size, new double[] { 0.3, 0.3, 0.3, 1 }, new double[] { 0.7, 0.7, 0.7, 1 });
                otherTexture = new LoadedTexture(capi, capi.Gui.LoadCairoTexture(surface, false), size / 2, size / 2);
                ctx.Dispose();
                surface.Dispose();
            }



            foreach (IPlayer player in capi.World.AllOnlinePlayers)
            {

                if (MapComps.TryGetValue(player, out CANEntityMapComponent cmp))
                {
                    cmp?.Dispose();
                    MapComps.Remove(player);
                }


                if (player.Entity == null)
                {
                    capi.World.Logger.Warning("Can't add player {0} to world map, missing entity :<", player.PlayerUID);
                    continue;
                }

                if (capi.World.Config.GetBool("mapHideOtherPlayers", false) && player.PlayerUID != capi.World.Player.PlayerUID) continue;


                cmp = new CANEntityMapComponent(capi, player == capi.World.Player ? ownTexture : otherTexture, player.Entity);

                MapComps[player] = cmp;
            }
        }


        public override void Render(CANGuiElementMap mapElem, float dt)
        {
            if (!Active) return;

            foreach (var val in MapComps)
            {
                val.Value.Render(mapElem, dt);
            }
        }

        public override void OnMouseMoveClient(MouseEvent args, CANGuiElementMap mapElem, StringBuilder hoverText)
        {
            if (!Active) return;

            foreach (var val in MapComps)
            {
                val.Value.OnMouseMove(args, mapElem, hoverText);
            }
        }

        public override void OnMouseUpClient(MouseEvent args, CANGuiElementMap mapElem)
        {
            if (!Active) return;

            foreach (var val in MapComps)
            {
                val.Value.OnMouseUpOnElement(args, mapElem);
            }
        }

        public override void OnMapClosedClient()
        {
            //Dispose();
            //MapComps.Clear();
        }


        public override void Dispose()
        {
            foreach (var val in MapComps)
            {
                val.Value?.Dispose();
            }

            ownTexture?.Dispose();
            ownTexture = null;
            otherTexture?.Dispose();
            otherTexture = null;
        }
    }
}
