using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.gui.Map;
using CANAntiqueAtlas.src.gui.Map.WaypointLayer;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.gui
{
    public class CANGuiDialogWorldMap: GuiDialogGeneric
    {
        public long atlasId;
        public override bool PrefersUngrabbedMouse
        {
            get
            {
                return true;
            }
        }
        public override double DrawOrder
        {
            get
            {
                return 0.07;
            }
        }
        public float SavedZoom = 1;
        public CANGuiDialogWorldMap(OnViewChangedDelegate viewChanged, OnViewChangedSyncDelegate viewChangedSync, ICoreClientAPI capi, List<string> tabnames)
            : base("", capi)
        {
            this.viewChanged = viewChanged;
            this.viewChangedSync = viewChangedSync;
            this.tabnames = tabnames;
            this.fullDialog = this.ComposeDialog(EnumDialogType.Dialog);
            CommandArgumentParsers parsers = capi.ChatCommands.Parsers;
            capi.ChatCommands.GetOrCreate("map").BeginSubCommand("worldmapsize").WithDescription("Show/set worldmap size")
                .WithArgs(new ICommandArgumentParser[]
                {
                    parsers.OptionalInt("mapWidth", 1200),
                    parsers.OptionalInt("mapHeight", 800)
                })
                .HandleWith(new OnCommandDelegate(this.OnCmdMapSize));
        }
        private TextCommandResult OnCmdMapSize(TextCommandCallingArgs args)
        {
            DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
            if (args.Parsers[0].IsMissing)
            {
                defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 2);
                defaultInterpolatedStringHandler.AppendLiteral("Current map size: ");
                defaultInterpolatedStringHandler.AppendFormatted<int>(this.mapWidth);
                defaultInterpolatedStringHandler.AppendLiteral("x");
                defaultInterpolatedStringHandler.AppendFormatted<int>(this.mapHeight);
                return TextCommandResult.Success(defaultInterpolatedStringHandler.ToStringAndClear(), null);
            }
            this.mapWidth = (int)args.Parsers[0].GetValue();
            this.mapHeight = (int)args.Parsers[1].GetValue();
            this.fullDialog = this.ComposeDialog(EnumDialogType.Dialog);
            defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 2);
            defaultInterpolatedStringHandler.AppendLiteral("Map size ");
            defaultInterpolatedStringHandler.AppendFormatted<int>(this.mapWidth);
            defaultInterpolatedStringHandler.AppendLiteral("x");
            defaultInterpolatedStringHandler.AppendFormatted<int>(this.mapHeight);
            defaultInterpolatedStringHandler.AppendLiteral(" set");
            return TextCommandResult.Success(defaultInterpolatedStringHandler.ToStringAndClear(), null);
        }
        private GuiComposer ComposeDialog(EnumDialogType dlgType)
        {
            //this.mapWidth = (int)ElementBounds.scaled(310);
            ElementBounds mapBounds = ElementBounds.Fixed(0.0, 0, (double)930 * RuntimeEnv.GUIScale, (double)654 * RuntimeEnv.GUIScale);
            ElementBounds layerList = mapBounds.RightCopy(0.0, 0.0, 0.0, 0.0).WithFixedSize(1.0, 350.0);
            ElementBounds bgBounds = ElementBounds.Fill;
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(new ElementBounds[] { mapBounds });
            //capi.Gui.sc
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);
            GuiComposer compo;

            compo = this.fullDialog;
            
            Cuboidd beforeBounds = null;
            if (compo != null)
            {
                CANGuiElementMap guiElementMap = compo.GetElement("mapElem") as CANGuiElementMap;
                beforeBounds = ((guiElementMap != null) ? guiElementMap.CurrentBlockViewBounds : null);
                compo.Dispose();
            }
            this.tabs = new List<GuiTab>();
            int i;
            bool? flag2;
            int j;
            for (i = 0; i < this.tabnames.Count; i = j + 1)
            {
                List<GuiTab> list = this.tabs;
                GuiTab guiTab = new GuiTab();
                guiTab.Name = Lang.Get("maplayer-" + this.tabnames[i], Array.Empty<object>());
                guiTab.DataInt = i;
                List<CANMapLayer> mapLayers = this.MapLayers;
                bool? flag;
                if (mapLayers == null)
                {
                    flag = null;
                }
                else
                {
                    CANMapLayer mapLayer = mapLayers.FirstOrDefault((CANMapLayer layer) => layer.LayerGroupCode == this.tabnames[i]);
                    flag = ((mapLayer != null) ? new bool?(mapLayer.Active) : null);
                }
                flag2 = flag;
                guiTab.Active = flag2.GetValueOrDefault(true);
                list.Add(guiTab);
                j = i;
            }
            //ElementBounds tabBounds = ElementBounds.Fixed(-200.0, 45.0, 200.0, 545.0);
            /*compo = this.capi.Gui.CreateCompo("worldmap" + dlgType.ToString(), dialogBounds).AddShadedDialogBG(bgBounds, false, 5.0, 0.75f).AddIf(dlgType == EnumDialogType.Dialog)
                .AddDialogTitleBar(Lang.Get("World Map", Array.Empty<object>()), new Action(this.OnTitleBarClose), null, null, null)
                .AddInset(mapBounds, 2, 0.85f)
                .EndIf()
                .BeginChildElements(bgBounds)
                .AddHoverText("", CairoFont.WhiteDetailText(), 350, mapBounds.FlatCopy(), "hoverText")
                .AddIf(dlgType == EnumDialogType.Dialog)
                .AddVerticalToggleTabs(this.tabs.ToArray(), tabBounds, new Action<int, GuiTab>(this.OnTabClicked), "verticalTabs")
                .EndIf()
                .EndChildElements();*/
           /* var p = mapBounds.FlatCopy();
            ElementBounds imageBounds = ElementBounds.FixedSize(500, 500);
            p.WithChild(imageBounds);*/
            /*compo.AddImageBG(mapBounds, new AssetLocation("canantiqueatlas:gui/book.png"), scale: 0.259f);
            var c = mapBounds.FlatCopy();
            c.fixedX += 120;
            c.fixedY += 140;
            compo.AddImage(c, new AssetLocation("canantiqueatlas:gui/tiles/birch3.png"));*/
            
            //compo.Compose(true);
             List<CANMapLayer> maplayers = this.capi.ModLoader.GetModSystem<CANWorldMapManager>(true).MapLayers;
            compo = this.capi.Gui.CreateCompo("worldmap" + dlgType.ToString(), dialogBounds);
            var c = RuntimeEnv.GUIScale;
            /*
            0.875 -   7 - 0.499
            1 -       8 0.499
            1.125 -   9 0.495
            1.25 -    10 0.5
             
             */
            var sc = (float)ElementBounds.scaled(0.5f);
            //compo.AddShadedDialogBG(bgBounds, false, 5.0, 0.75f);
            //0.4985f - with scaled sizes 0.5 - 8 
            //0.445f -    0.5625 - 9
            //0.4 - 0.625 - - 10
            var ff = (0.5f / (float)RuntimeEnv.GUIScale);
            compo.AddImageBG(bgBounds, new AssetLocation("canantiqueatlas:gui/book.png"), scale: (0.3333f / (float)RuntimeEnv.GUIScale) - 0.001f);
            //compo.AddImage
            var innerMap = mapBounds.FlatCopy();
            var offsetFull = 36 * (float)RuntimeEnv.GUIScale;
            innerMap.fixedWidth -= offsetFull;
            innerMap.fixedHeight -= offsetFull;
            innerMap.fixedOffsetX += offsetFull / 2;
            innerMap.fixedOffsetY += offsetFull / 2;
            compo.AddIf(dlgType == EnumDialogType.Dialog)
                //.AddDialogTitleBar(Lang.Get("World Map", Array.Empty<object>()), new Action(this.OnTitleBarClose), null, null, null)
                //.AddInset(mapBounds, 2, 0.85f)
                .EndIf()
                .BeginChildElements(bgBounds)
                //.AddHoverText("", CairoFont.WhiteDetailText(), 350, mapBounds.FlatCopy(), "hoverText")
                .AddIf(dlgType == EnumDialogType.Dialog)
                //ddVerticalToggleTabs(this.tabs.ToArray(), tabBounds, new Action<int, GuiTab>(this.OnTabClicked), "verticalTabs")
                .EndIf()
                .AddInteractiveElement(new CANGuiElementMap(maplayers, this.capi, this, innerMap, dlgType == EnumDialogType.HUD), "mapElem")
                .EndChildElements()
                .Compose(true);
            GuiTab guiTab2 = this.tabs[0];
            List<CANMapLayer> mapLayers2 = this.MapLayers;
            bool? flag3;
            if (mapLayers2 == null)
            {
                flag3 = null;
            }
            else
            {
                CANMapLayer mapLayer2 = mapLayers2.FirstOrDefault((CANMapLayer layer) => layer.LayerGroupCode == this.tabnames[0]);
                flag3 = ((mapLayer2 != null) ? new bool?(mapLayer2.Active) : null);
            }
            flag2 = flag3;
            guiTab2.Active = flag2.GetValueOrDefault(true);
            compo.OnComposed += this.OnRecomposed;
            CANGuiElementMap mapElem = compo.GetElement("mapElem") as CANGuiElementMap;
            if (beforeBounds != null)
            {
                mapElem.chunkViewBoundsBefore = beforeBounds.ToCuboidi().Div(32);
            }
            mapElem.viewChanged = this.viewChanged;
            mapElem.viewChangedSync = this.viewChangedSync;
            
            /*if(SavedZoom != 1f)
            {
                mapElem.ZoomAdd(SavedZoom - 1f, 0.5f, 0.5f);
                SavedZoom = mapElem.ZoomLevel;
            }*/
            mapElem.ZoomAdd(1f, 0.5f, 0.5f);
            //compo.GetHoverText("hoverText").SetAutoWidth(true);
            if (this.listenerId == 0L)
            {
                this.listenerId = this.capi.Event.RegisterGameTickListener(delegate (float dt)
                {
                    if (!this.IsOpened())
                    {
                        return;
                    }
                    CANGuiElementMap guiElementMap2 = base.SingleComposer.GetElement("mapElem") as CANGuiElementMap;
                    if (guiElementMap2 != null)
                    {
                        guiElementMap2.EnsureMapFullyLoaded();
                    }
                    if (this.requireRecompose)
                    {
                        this.capi.ModLoader.GetModSystem<CANWorldMapManager>(true).ToggleMap(EnumDialogType.Dialog, this.atlasId);
                        //this.capi.ModLoader.GetModSystem<CANWorldMapManager>(true).ToggleMap(dlgtype);
                        this.requireRecompose = false;
                    }
                }, 100, 0);
            }
            if (dlgType == EnumDialogType.Dialog)
            {
                foreach (CANMapLayer mapLayer3 in maplayers)
                {
                    mapLayer3.ComposeDialogExtras(this, compo);
                }
            }
            this.capi.World.FrameProfiler.Mark("composeworldmap");
            this.updateMaplayerExtrasState();
            return compo;
        }
        private void OnTabClicked(int arg1, GuiTab tab)
        {
            string layerGroupCode = this.tabnames[arg1];
            foreach (CANMapLayer ml in this.MapLayers)
            {
                if (ml.LayerGroupCode == layerGroupCode)
                {
                    ml.Active = tab.Active;
                }
            }
            this.updateMaplayerExtrasState();
        }
        private void updateMaplayerExtrasState()
        {
            if (this.tabs == null)
            {
                return;
            }
            for (int i = 0; i < this.tabs.Count; i++)
            {
                string layerGroupCode = this.tabnames[i];
                GuiTab tab = this.tabs[i];
                if (this.Composers["worldmap-layer-" + layerGroupCode] != null)
                {
                    this.Composers["worldmap-layer-" + layerGroupCode].Enabled = tab.Active;
                }
            }
        }
        private void OnRecomposed()
        {
            this.requireRecompose = true;
        }
        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            this.updateMaplayerExtrasState();

            
            base.SingleComposer = this.ComposeDialog(EnumDialogType.Dialog);
            
            CANGuiElementMap mapElem = base.SingleComposer.GetElement("mapElem") as CANGuiElementMap;
            if (mapElem != null)
            {
                mapElem.chunkViewBoundsBefore = new Cuboidi();
            }
            this.OnMouseMove(new MouseEvent(this.capi.Input.MouseX, this.capi.Input.MouseY));
        }
        private void OnTitleBarClose()
        {
            this.TryClose();
        }
        public override bool TryClose()
        {
            //if (this.DialogType == EnumDialogType.Dialog && this.capi.Settings.Bool["showMinimapHud"])
            //{
                //this.Open(EnumDialogType.HUD);
                //return false;
            //}
            return base.TryClose();
        }
        public void Open(EnumDialogType type)
        {
            //this.opened = false;
            this.TryOpen();
        }
        public override void OnGuiClosed()
        {
            this.updateMaplayerExtrasState();
            base.OnGuiClosed();
        }
        public override void Dispose()
        {
            base.Dispose();
            this.capi.Event.UnregisterGameTickListener(this.listenerId);
            this.listenerId = 0L;
            this.fullDialog.Dispose();
        }

        // Token: 0x1700015E RID: 350
        // (get) Token: 0x06000AC1 RID: 2753 RVA: 0x00060ABA File Offset: 0x0005ECBA
        public List<CANMapLayer> MapLayers
        {
            get
            {
                GuiComposer singleComposer = base.SingleComposer;
                CANGuiElementMap guiElementMap = ((singleComposer != null) ? singleComposer.GetElement("mapElem") : null) as CANGuiElementMap;
                if (guiElementMap == null)
                {
                    return null;
                }
                return guiElementMap.mapLayers;
            }
        }
        public override void OnMouseMove(MouseEvent args)
        {
            base.OnMouseMove(args);
            if (base.SingleComposer != null && base.SingleComposer.Bounds.PointInside(args.X, args.Y))
            {
                this.loadWorldPos((double)args.X, (double)args.Y, ref this.hoveredWorldPos);
                double yAbs = this.hoveredWorldPos.Y;
                this.hoveredWorldPos.Sub(this.capi.World.DefaultSpawnPosition.AsBlockPos);
                this.hoveredWorldPos.Y = yAbs;
                StringBuilder hoverText = new StringBuilder();
                hoverText.AppendLine(string.Format("{0}, {1}, {2}", (int)this.hoveredWorldPos.X, (int)this.hoveredWorldPos.Y, (int)this.hoveredWorldPos.Z));

                CANGuiElementMap mpc = base.SingleComposer.GetElement("mapElem") as CANGuiElementMap;
                GuiElementHoverText hoverTextElem = base.SingleComposer.GetHoverText("hoverText");
                foreach (CANMapLayer mapLayer in mpc.mapLayers)
                {
                    mapLayer.OnMouseMoveClient(args, mpc, hoverText);
                }
                string text = hoverText.ToString().TrimEnd();
                //hoverTextElem.SetNewText(text);
            }
        }
        private void loadWorldPos(double mouseX, double mouseY, ref Vec3d worldPos)
        {
            double x = mouseX - base.SingleComposer.Bounds.absX;
            double y = mouseY - base.SingleComposer.Bounds.absY - GuiElement.scaled(30.0);

            (base.SingleComposer.GetElement("mapElem") as CANGuiElementMap).TranslateViewPosToWorldPos(new Vec2f((float)x, (float)y), ref worldPos);
            worldPos.Y += 1.0;
        }
        public override void OnMouseDown(MouseEvent args)
        {
            base.OnMouseDown(args);
        }
        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);
            this.capi.Render.CheckGlError("map-rend2d");
        }
        public override void OnFinalizeFrame(float dt)
        {
            base.OnFinalizeFrame(dt);
            this.capi.Render.CheckGlError("map-fina");
            bool showHover = base.SingleComposer.Bounds.PointInside(this.capi.Input.MouseX, this.capi.Input.MouseY) && this.Focused;
            GuiElementHoverText hoverText = base.SingleComposer.GetHoverText("hoverText");
            //hoverText.SetVisible(showHover);
            //hoverText.SetAutoDisplay(showHover);
        }
        public void TranslateWorldPosToViewPos(Vec3d worldPos, ref Vec2f viewPos)
        {
            (base.SingleComposer.GetElement("mapElem") as CANGuiElementMap).TranslateWorldPosToViewPos(worldPos, ref viewPos);
        }
        public override void OnMouseUp(MouseEvent args)
        {
            if (!base.SingleComposer.Bounds.PointInside(args.X, args.Y))
            {
                base.OnMouseUp(args);
                return;
            }
            CANGuiElementMap mpc = base.SingleComposer.GetElement("mapElem") as CANGuiElementMap;
            foreach (CANMapLayer mapLayer in mpc.mapLayers)
            {
                mapLayer.OnMouseUpClient(args, mpc);
                if (args.Handled)
                {
                    return;
                }
            }
            //TODO
            if (args.Button == EnumMouseButton.Right)
            {
                if(!CANAntiqueAtlas.capi.World.Player.Entity.Controls.ShiftKey)
                {
                    return;
                }
                Vec3d wpPos = new Vec3d();
                this.loadWorldPos((double)args.X, (double)args.Y, ref wpPos);
                if (this.addWpDlg != null)
                {
                    this.addWpDlg.TryClose();
                    this.addWpDlg.Dispose();
                }
                CANWaypointMapLayer wml = this.MapLayers.FirstOrDefault((CANMapLayer l) => l is CANWaypointMapLayer) as CANWaypointMapLayer;
                this.addWpDlg = new CANGuiDialogAddWayPoint(this.capi, wml);
                this.addWpDlg.WorldPos = wpPos;
                this.addWpDlg.TryOpen();
                this.addWpDlg.OnClosed += delegate
                {
                    this.capi.Gui.RequestFocus(this);
                };
            }
            base.OnMouseUp(args);
        }
        public override bool ShouldReceiveKeyboardEvents()
        {
            return base.ShouldReceiveKeyboardEvents();
        }
        private EnumDialogArea GetMinimapPosition(out double offsetX, out double offsetY)
        {
            offsetX = GuiStyle.DialogToScreenPadding;
            offsetY = GuiStyle.DialogToScreenPadding;
            EnumDialogArea position;
            switch (this.capi.Settings.Int["minimapHudPosition"])
            {
                case 1:
                    position = EnumDialogArea.LeftTop;
                    break;
                case 2:
                    position = EnumDialogArea.LeftBottom;
                    offsetY = -offsetY;
                    break;
                case 3:
                    position = EnumDialogArea.RightBottom;
                    offsetX = -offsetX;
                    offsetY = -offsetY;
                    break;
                default:
                    position = EnumDialogArea.RightTop;
                    offsetX = -offsetX;
                    break;
            }
            return position;
        }
        protected OnViewChangedDelegate viewChanged;
        protected OnViewChangedSyncDelegate viewChangedSync;
        protected long listenerId;
        protected bool requireRecompose;

        // Token: 0x04000995 RID: 2453
        protected int mapWidth = 1200;

        // Token: 0x04000996 RID: 2454
        protected int mapHeight = 800;

        // Token: 0x04000997 RID: 2455
        protected GuiComposer fullDialog;
        protected List<GuiTab> tabs;
        private List<string> tabnames;
        private Vec3d hoveredWorldPos = new Vec3d();
        private CANGuiDialogAddWayPoint addWpDlg;
    }
}
