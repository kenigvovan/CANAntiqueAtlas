using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.gui.Map
{
    public abstract class CANMarkerMapLayer: CANMapLayer
    {
        public Dictionary<string, int> IconTextures = new Dictionary<string, int>();

        public CANMarkerMapLayer(ICoreAPI api, ICANWorldMapManager mapSink)
            : base(api, mapSink)
        {
        }
    }
}
