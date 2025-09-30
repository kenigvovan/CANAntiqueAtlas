using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.gui.Map.TileLayer
{
    public abstract class CANRGBMapLayer: CANMapLayer
    {
        public Dictionary<FastVec2i, int> ChunkTextures = new Dictionary<FastVec2i, int>();

        public bool Visible;

        public abstract MapLegendItem[] LegendItems { get; }

        public abstract EnumMinMagFilter MinFilter { get; }

        public abstract EnumMinMagFilter MagFilter { get; }

        public CANRGBMapLayer(ICoreAPI api, ICANWorldMapManager mapSink)
            : base(api, mapSink)
        {
        }
    }
}
