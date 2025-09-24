using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.gui
{
    public class CANGuiElementMap: GuiElement
    {
        public List<CANMapLayer> mapLayers;

        public bool IsDragingMap;

        public float ZoomLevel = 1f;

        internal Vec3d prevPlayerPos = new Vec3d();

        public Cuboidi chunkViewBoundsBefore = new Cuboidi();

        public OnViewChangedDelegate viewChanged;

        public OnViewChangedSyncDelegate viewChangedSync;

        private bool snapToPlayer;

        public Cuboidd CurrentBlockViewBounds = new Cuboidd();

        private CANGuiDialogWorldMap worldmapdlg;

        private float tkeyDeltaX;

        private float tkeyDeltaY;

        private float skeyDeltaX;

        private float skeyDeltaY;

        private int prevMouseX;

        private int prevMouseY;

        private List<FastVec2i> nowVisible = new List<FastVec2i>();

        private List<FastVec2i> nowHidden = new List<FastVec2i>();

        public ICoreClientAPI Api => api;

        private bool dialogHasFocus
        {
            get
            {
                if (worldmapdlg.Focused)
                {
                    return worldmapdlg.DialogType == EnumDialogType.Dialog;
                }

                return false;
            }
        }

        public CANGuiElementMap(List<CANMapLayer> mapLayers, ICoreClientAPI capi, CANGuiDialogWorldMap worldmapdlg, ElementBounds bounds, bool snapToPlayer)
            : base(capi, bounds)
        {
            this.mapLayers = mapLayers;
            this.snapToPlayer = snapToPlayer;
            this.worldmapdlg = worldmapdlg;
            prevPlayerPos.X = api.World.Player.Entity.Pos.X;
            prevPlayerPos.Z = api.World.Player.Entity.Pos.Z;
        }

        public override void ComposeElements(Context ctxStatic, ImageSurface surface)
        {
            Bounds.CalcWorldBounds();
            chunkViewBoundsBefore = new Cuboidi();
            BlockPos asBlockPos = api.World.Player.Entity.Pos.AsBlockPos;
            CurrentBlockViewBounds = new Cuboidd((double)asBlockPos.X - Bounds.InnerWidth / 2.0 / (double)ZoomLevel, 0.0, (double)asBlockPos.Z - Bounds.InnerHeight / 2.0 / (double)ZoomLevel, (double)asBlockPos.X + Bounds.InnerWidth / 2.0 / (double)ZoomLevel, 0.0, (double)asBlockPos.Z + Bounds.InnerHeight / 2.0 / (double)ZoomLevel);
        }

        public override void RenderInteractiveElements(float deltaTime)
        {
            api.Render.PushScissor(Bounds);
            for (int i = 0; i < mapLayers.Count; i++)
            {
                mapLayers[i].Render(this, deltaTime);
            }

            api.Render.PopScissor();
            api.Render.CheckGlError();
        }

        public override void PostRenderInteractiveElements(float deltaTime)
        {
            base.PostRenderInteractiveElements(deltaTime);
            EntityPlayer entity = api.World.Player.Entity;
            double num = entity.Pos.X - prevPlayerPos.X;
            double num2 = entity.Pos.Z - prevPlayerPos.Z;
            if (Math.Abs(num) > 0.0002 || Math.Abs(num2) > 0.0002)
            {
                if (snapToPlayer)
                {
                    EntityPos pos = api.World.Player.Entity.Pos;
                    CurrentBlockViewBounds.X1 = pos.X - Bounds.InnerWidth / 2.0 / (double)ZoomLevel;
                    CurrentBlockViewBounds.Z1 = pos.Z - Bounds.InnerHeight / 2.0 / (double)ZoomLevel;
                    CurrentBlockViewBounds.X2 = pos.X + Bounds.InnerWidth / 2.0 / (double)ZoomLevel;
                    CurrentBlockViewBounds.Z2 = pos.Z + Bounds.InnerHeight / 2.0 / (double)ZoomLevel;
                }
                else
                {
                    CurrentBlockViewBounds.Translate(num, 0.0, num2);
                }
            }

            prevPlayerPos.Set(entity.Pos.X, entity.Pos.Y, entity.Pos.Z);
            if (dialogHasFocus)
            {
                if (api.Input.KeyboardKeyStateRaw[45])
                {
                    tkeyDeltaY = 15f;
                }
                else if (api.Input.KeyboardKeyStateRaw[46])
                {
                    tkeyDeltaY = -15f;
                }
                else
                {
                    tkeyDeltaY = 0f;
                }

                if (api.Input.KeyboardKeyStateRaw[47])
                {
                    tkeyDeltaX = 15f;
                }
                else if (api.Input.KeyboardKeyStateRaw[48])
                {
                    tkeyDeltaX = -15f;
                }
                else
                {
                    tkeyDeltaX = 0f;
                }

                skeyDeltaX += (tkeyDeltaX - skeyDeltaX) * deltaTime * 15f;
                skeyDeltaY += (tkeyDeltaY - skeyDeltaY) * deltaTime * 15f;
                if (Math.Abs(skeyDeltaX) > 0.5f || Math.Abs(skeyDeltaY) > 0.5f)
                {
                    CurrentBlockViewBounds.Translate((0f - skeyDeltaX) / ZoomLevel, 0.0, (0f - skeyDeltaY) / ZoomLevel);
                }
            }
        }

        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseDownOnElement(api, args);
            if (args.Button == EnumMouseButton.Left)
            {
                IsDragingMap = true;
                prevMouseX = args.X;
                prevMouseY = args.Y;
            }
        }

        public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseUp(api, args);
            IsDragingMap = false;
        }

        public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
        {
            if (IsDragingMap)
            {
                CurrentBlockViewBounds.Translate((float)(-(args.X - prevMouseX)) / ZoomLevel, 0.0, (float)(-(args.Y - prevMouseY)) / ZoomLevel);
                prevMouseX = args.X;
                prevMouseY = args.Y;
            }
        }

        public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
        {
            if (Bounds.ParentBounds.PointInside(api.Input.MouseX, api.Input.MouseY))
            {
                float px = (float)(((double)api.Input.MouseX - Bounds.absX) / Bounds.InnerWidth);
                float pz = (float)(((double)api.Input.MouseY - Bounds.absY) / Bounds.InnerHeight);
                ZoomAdd((args.delta > 0) ? 0.25f : (-0.25f), px, pz);
                args.SetHandled();
            }
        }

        public void ZoomAdd(float zoomDiff, float px, float pz)
        {
            if ((!(zoomDiff < 0f) || !(ZoomLevel + zoomDiff < 0.25f)) && (!(zoomDiff > 0f) || !(ZoomLevel + zoomDiff > 6f)))
            {
                ZoomLevel += zoomDiff;
                double num = 1f / ZoomLevel;
                double num2 = Bounds.InnerWidth * num - CurrentBlockViewBounds.Width;
                double num3 = Bounds.InnerHeight * num - CurrentBlockViewBounds.Length;
                CurrentBlockViewBounds.X2 += num2;
                CurrentBlockViewBounds.Z2 += num3;
                CurrentBlockViewBounds.Translate((0.0 - num2) * (double)px, 0.0, (0.0 - num3) * (double)pz);
                EnsureMapFullyLoaded();
            }
        }

        public void TranslateWorldPosToViewPos(Vec3d worldPos, ref Vec2f viewPos)
        {
            if (worldPos == null)
            {
                throw new ArgumentNullException("worldPos is null");
            }

            double num = CurrentBlockViewBounds.X2 - CurrentBlockViewBounds.X1;
            double num2 = CurrentBlockViewBounds.Z2 - CurrentBlockViewBounds.Z1;
            viewPos.X = (float)((worldPos.X - CurrentBlockViewBounds.X1) / num * Bounds.InnerWidth);
            viewPos.Y = (float)((worldPos.Z - CurrentBlockViewBounds.Z1) / num2 * Bounds.InnerHeight);
        }

        public void ClampButPreserveAngle(ref Vec2f viewPos, int border)
        {
            if (!(viewPos.X >= (float)border) || !((double)viewPos.X <= Bounds.InnerWidth - 2.0) || !(viewPos.Y >= (float)border) || !((double)viewPos.Y <= Bounds.InnerHeight - 2.0))
            {
                double num = Bounds.InnerWidth / 2.0 - (double)border;
                double num2 = Bounds.InnerHeight / 2.0 - (double)border;
                double value = ((double)viewPos.X - num) / num;
                double num3 = Math.Max(val2: Math.Abs(((double)viewPos.Y - num2) / num2), val1: Math.Abs(value));
                viewPos.X = (float)(((double)viewPos.X - num) / num3 + num);
                viewPos.Y = (float)(((double)viewPos.Y - num2) / num3 + num2);
            }
        }

        public void TranslateViewPosToWorldPos(Vec2f viewPos, ref Vec3d worldPos)
        {
            if (worldPos == null)
            {
                throw new ArgumentNullException("viewPos is null");
            }

            double num = CurrentBlockViewBounds.X2 - CurrentBlockViewBounds.X1;
            double num2 = CurrentBlockViewBounds.Z2 - CurrentBlockViewBounds.Z1;
            worldPos.X = (double)viewPos.X * num / Bounds.InnerWidth + CurrentBlockViewBounds.X1;
            worldPos.Z = (double)viewPos.Y * num2 / Bounds.InnerHeight + CurrentBlockViewBounds.Z1;
            worldPos.Y = api.World.BlockAccessor.GetRainMapHeightAt(worldPos.AsBlockPos);
        }

        public void EnsureMapFullyLoaded()
        {
            nowVisible.Clear();
            nowHidden.Clear();
            Cuboidi cuboidi = CurrentBlockViewBounds.ToCuboidi();
            cuboidi.Div(32);
            if (chunkViewBoundsBefore.Equals(cuboidi))
            {
                return;
            }

            viewChangedSync(cuboidi.X1, cuboidi.Z1, cuboidi.X2, cuboidi.Z2);
            BlockPos blockPos = new BlockPos();
            bool flag = chunkViewBoundsBefore.SizeX == 0 && chunkViewBoundsBefore.SizeZ == 0;
            int num = ((cuboidi.X2 > chunkViewBoundsBefore.X2) ? 1 : (-1));
            int num2 = ((cuboidi.Z2 > chunkViewBoundsBefore.Z2) ? 1 : (-1));
            blockPos.Set((num > 0) ? cuboidi.X1 : cuboidi.X2, 0, cuboidi.Z1);
            while ((num > 0 && blockPos.X <= cuboidi.X2) || (num < 0 && blockPos.X >= cuboidi.X1))
            {
                blockPos.Z = ((num2 > 0) ? cuboidi.Z1 : cuboidi.Z2);
                while ((num2 > 0 && blockPos.Z <= cuboidi.Z2) || (num2 < 0 && blockPos.Z >= cuboidi.Z1))
                {
                    if (flag || !chunkViewBoundsBefore.ContainsOrTouches(blockPos))
                    {
                        nowVisible.Add(new FastVec2i(blockPos.X, blockPos.Z));
                    }

                    blockPos.Z += num2;
                }

                blockPos.X += num;
            }

            if (!flag)
            {
                blockPos.Set(chunkViewBoundsBefore.X1, 0, chunkViewBoundsBefore.Z1);
                while (blockPos.X <= chunkViewBoundsBefore.X2)
                {
                    blockPos.Z = chunkViewBoundsBefore.Z1;
                    while (blockPos.Z <= chunkViewBoundsBefore.Z2)
                    {
                        if (!cuboidi.ContainsOrTouches(blockPos))
                        {
                            nowHidden.Add(new FastVec2i(blockPos.X, blockPos.Z));
                        }

                        blockPos.Z++;
                    }

                    blockPos.X++;
                }
            }

            chunkViewBoundsBefore = cuboidi.Clone();
            if (nowHidden.Count > 0 || nowVisible.Count > 0)
            {
                viewChanged(nowVisible, nowHidden);
            }
        }

        public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
        {
            base.OnKeyDown(api, args);
            if (args.KeyCode == 51)
            {
                CenterMapTo(api.World.Player.Entity.Pos.AsBlockPos);
            }

            if (api.Input.KeyboardKeyStateRaw[45] || api.Input.KeyboardKeyStateRaw[46] || api.Input.KeyboardKeyStateRaw[47] || api.Input.KeyboardKeyStateRaw[48])
            {
                args.Handled = true;
            }

            if (api.Input.KeyboardKeyStateRaw[121] || api.Input.KeyboardKeyStateRaw[80])
            {
                ZoomAdd(0.25f, 0.5f, 0.5f);
            }

            if (api.Input.KeyboardKeyStateRaw[120] || api.Input.KeyboardKeyStateRaw[79])
            {
                ZoomAdd(-0.25f, 0.5f, 0.5f);
            }
        }

        public override void OnKeyUp(ICoreClientAPI api, KeyEvent args)
        {
            base.OnKeyUp(api, args);
        }

        public void CenterMapTo(BlockPos pos)
        {
            CurrentBlockViewBounds = new Cuboidd((double)pos.X - Bounds.InnerWidth / 2.0 / (double)ZoomLevel, 0.0, (double)pos.Z - Bounds.InnerHeight / 2.0 / (double)ZoomLevel, (double)pos.X + Bounds.InnerWidth / 2.0 / (double)ZoomLevel, 0.0, (double)pos.Z + Bounds.InnerHeight / 2.0 / (double)ZoomLevel);
        }

        public override void Dispose()
        {
        }
    }
}
