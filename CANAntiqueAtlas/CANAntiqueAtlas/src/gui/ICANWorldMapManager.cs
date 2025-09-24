using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.gui
{
    public interface ICANWorldMapManager
    {
        bool IsShuttingDown { get; }

        bool IsOpened { get; }

        void TranslateWorldPosToViewPos(Vec3d worldPos, ref Vec2f viewPos);

        void SendMapDataToClient(CANMapLayer forMapLayer, IServerPlayer forPlayer, byte[] data);

        void SendMapDataToServer(CANMapLayer forMapLayer, byte[] data);
    }
}
