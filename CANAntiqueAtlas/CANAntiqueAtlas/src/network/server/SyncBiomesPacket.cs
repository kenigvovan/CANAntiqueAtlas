using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using static HarmonyLib.Code;
using static Vintagestory.Client.NoObf.ClientPlatformWindows;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.network.server
{
    /**
     * Packet used to save the last browsing position for a dimension in an atlas.
     * @author Hunternif
     */
    [ProtoContract]
    public class SyncBiomesPacket
    {
        [ProtoMember(1)]
        public Dictionary<string, int> BiomesMapping;
    }
}
