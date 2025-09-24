using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.gui
{
    public abstract class CANMapLayer
    {
        public string RequirePrivilege;

        public string RequireCode;

        public EnumGameMode? RequiredGameMode;

        public int ZIndex = 1;

        protected ICoreAPI api;

        protected ICANWorldMapManager mapSink;

        public HashSet<Vec2i> LoadedChunks = new HashSet<Vec2i>();

        public abstract string Title { get; }

        public abstract string LayerGroupCode { get; }

        public bool Active { get; set; }

        public abstract EnumMapAppSide DataSide { get; }

        public virtual bool RequireChunkLoaded => true;

        public CANMapLayer(ICoreAPI api, ICANWorldMapManager mapSink)
        {
            this.api = api;
            this.mapSink = mapSink;
            Active = true;
        }

        public virtual void OnOffThreadTick(float dt)
        {
        }

        public virtual void OnTick(float dt)
        {
        }

        public virtual void OnViewChangedClient(List<FastVec2i> nowVisible, List<FastVec2i> nowHidden)
        {
        }

        [Obsolete("Receiving the OnViewChangedPacket now calls: OnViewChangedServer(fromPlayer, int x1, int z1, int x2, int z2) but retained in 1.20.10 for backwards compatibility")]
        public virtual void OnViewChangedServer(IServerPlayer fromPlayer, List<FastVec2i> nowVisible, List<FastVec2i> nowHidden)
        {
        }

        public virtual void OnViewChangedServer(IServerPlayer fromPlayer, int x1, int z1, int x2, int z2)
        {
        }

        public virtual void OnMapOpenedClient()
        {
        }

        public virtual void OnMapClosedClient()
        {
        }

        public virtual void OnMapOpenedServer(IServerPlayer fromPlayer)
        {
        }

        public virtual void OnMapClosedServer(IServerPlayer fromPlayer)
        {
        }

        public virtual void OnDataFromServer(byte[] data)
        {
        }

        public virtual void OnDataFromClient(byte[] data)
        {
        }

        public virtual void OnLoaded()
        {
        }

        public virtual void Dispose()
        {
        }

        public virtual void OnShutDown()
        {
        }

        public virtual void Render(CANGuiElementMap mapElem, float dt)
        {
        }

        public virtual void OnMouseMoveClient(MouseEvent args, CANGuiElementMap mapElem, StringBuilder hoverText)
        {
        }

        public virtual void OnMouseUpClient(MouseEvent args, CANGuiElementMap mapElem)
        {
        }

        public virtual void ComposeDialogExtras(CANGuiDialogWorldMap guiDialogWorldMap, GuiComposer compo)
        {
        }
    }
}
