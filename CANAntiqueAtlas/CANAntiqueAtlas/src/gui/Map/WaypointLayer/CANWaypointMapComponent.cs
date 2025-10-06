﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.gui.Map.WaypointLayer
{
    public class CANWaypointMapComponent : CANMapComponent
    {
        Vec2f viewPos = new Vec2f();
        Vec4f color = new Vec4f();
        CANWaypoint waypoint;
        int waypointIndex;

        Matrixf mvMat = new Matrixf();

        CANWaypointMapLayer wpLayer;

        bool mouseOver;

        public static float IconScale = 0.85f;

        public CANWaypointMapComponent(int waypointIndex, CANWaypoint waypoint, CANWaypointMapLayer wpLayer, ICoreClientAPI capi) : base(capi)
        {
            this.waypointIndex = waypointIndex;
            this.waypoint = waypoint;
            this.wpLayer = wpLayer;

            ColorUtil.ToRGBAVec4f(waypoint.Color, ref color);
        }

        public override void Render(CANGuiElementMap map, float dt)
        {
            map.TranslateWorldPosToViewPos(waypoint.Position, ref viewPos);
            if (waypoint.Pinned)
            {
                map.Api.Render.PushScissor(null);
                map.ClampButPreserveAngle(ref viewPos, 2);
            }
            else
            {
                if (viewPos.X < -10 || viewPos.Y < -10 || viewPos.X > map.Bounds.OuterWidth + 10 || viewPos.Y > map.Bounds.OuterHeight + 10) return;
            }

            float x = (float)(map.Bounds.renderX + viewPos.X);
            float y = (float)(map.Bounds.renderY + viewPos.Y);

            ICoreClientAPI api = map.Api;

            IShaderProgram prog = api.Render.GetEngineShader(EnumShaderProgram.Gui);
            prog.Uniform("rgbaIn", color);
            prog.Uniform("extraGlow", 0);
            prog.Uniform("applyColor", 0);
            prog.Uniform("noTexture", 0f);


            float hover = (mouseOver ? 6 : 0) - 1.5f * Math.Max(1, 1 / map.ZoomLevel);

            if (!wpLayer.texturesByIcon.TryGetValue(waypoint.Icon, out LoadedTexture tex))
            {
                wpLayer.texturesByIcon.TryGetValue("circle", out tex);
            }

            if (tex != null)
            {
                prog.BindTexture2D("tex2d", tex.TextureId, 0);
                prog.UniformMatrix("projectionMatrix", api.Render.CurrentProjectionMatrix);
                mvMat
                    .Set(api.Render.CurrentModelviewMatrix)
                    .Translate(x, y, 60)
                    .Scale(tex.Width + hover, tex.Height + hover, 0)
                    .Scale(0.5f * IconScale, 0.5f * IconScale, 0)
                ;

                // Shadow
                var shadowMvMat = mvMat.Clone().Scale(1.25f, 1.25f, 1.25f);
                prog.Uniform("rgbaIn", new Vec4f(0, 0, 0, 0.6f));
                prog.UniformMatrix("modelViewMatrix", shadowMvMat.Values);
                api.Render.RenderMesh(wpLayer.quadModel);

                // Actual waypoint icon
                prog.Uniform("rgbaIn", color);
                prog.UniformMatrix("modelViewMatrix", mvMat.Values);
                api.Render.RenderMesh(wpLayer.quadModel);

            }

            if (waypoint.Pinned)
            {
                map.Api.Render.PopScissor();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            // Texture is disposed by WaypointMapLayer
        }



        public override void OnMouseMove(MouseEvent args, CANGuiElementMap mapElem, StringBuilder hoverText)
        {
            Vec2f viewPos = new Vec2f();
            mapElem.TranslateWorldPosToViewPos(waypoint.Position, ref viewPos);


            double x = viewPos.X + mapElem.Bounds.renderX;
            double y = viewPos.Y + mapElem.Bounds.renderY;

            if (waypoint.Pinned)
            {
                mapElem.ClampButPreserveAngle(ref viewPos, 2);
                x = viewPos.X + mapElem.Bounds.renderX;
                y = viewPos.Y + mapElem.Bounds.renderY;

                x = (float)GameMath.Clamp(x, mapElem.Bounds.renderX + 2, mapElem.Bounds.renderX + mapElem.Bounds.InnerWidth - 2);
                y = (float)GameMath.Clamp(y, mapElem.Bounds.renderY + 2, mapElem.Bounds.renderY + mapElem.Bounds.InnerHeight - 2);
            }
            double dX = args.X - x;
            double dY = args.Y - y;

            var size = RuntimeEnv.GUIScale * 8;
            if (mouseOver = Math.Abs(dX) < size && Math.Abs(dY) < size)
            {
                string text = Lang.Get("Waypoint {0}", waypointIndex) + "\n" + waypoint.Title;
                hoverText.AppendLine(text);
            }
        }

        CANGuiDialogEditWayPoint editWpDlg;
        public override void OnMouseUpOnElement(MouseEvent args, CANGuiElementMap mapElem)
        {
            if (args.Button == EnumMouseButton.Right)
            {
                Vec2f viewPos = new Vec2f();
                mapElem.TranslateWorldPosToViewPos(waypoint.Position, ref viewPos);

                double x = viewPos.X + mapElem.Bounds.renderX;
                double y = viewPos.Y + mapElem.Bounds.renderY;

                if (waypoint.Pinned)
                {
                    mapElem.ClampButPreserveAngle(ref viewPos, 2);
                    x = viewPos.X + mapElem.Bounds.renderX;
                    y = viewPos.Y + mapElem.Bounds.renderY;

                    x = (float)GameMath.Clamp(x, mapElem.Bounds.renderX + 2, mapElem.Bounds.renderX + mapElem.Bounds.InnerWidth - 2);
                    y = (float)GameMath.Clamp(y, mapElem.Bounds.renderY + 2, mapElem.Bounds.renderY + mapElem.Bounds.InnerHeight - 2);
                }

                double dX = args.X - x;
                double dY = args.Y - y;

                var size = RuntimeEnv.GUIScale * 8;
                if (Math.Abs(dX) < size && Math.Abs(dY) < size)
                {
                    if (editWpDlg != null)
                    {
                        editWpDlg.TryClose();
                        editWpDlg.Dispose();
                    }

                    if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative && capi.World.Player.Entity.Controls.ShiftKey)
                    {
                        var pos = waypoint.Position.AsBlockPos;
                        capi.SendChatMessage(string.Format("/tp ={0} {1} ={2}", pos.X, pos.Y, pos.Z));
                        mapElem.prevPlayerPos.Set(pos);
                        mapElem.CenterMapTo(pos);
                    }
                    else
                    {
                        var mapdlg = capi.ModLoader.GetModSystem<CANWorldMapManager>().worldMapDlg;

                        editWpDlg = new CANGuiDialogEditWayPoint(capi, mapdlg.MapLayers.FirstOrDefault(l => l is CANWaypointMapLayer) as CANWaypointMapLayer, waypoint, waypointIndex);
                        editWpDlg.TryOpen();
                        editWpDlg.OnClosed += () => capi.Gui.RequestFocus(mapdlg);
                    }

                    args.Handled = true;
                }
            }
        }
    }
}
