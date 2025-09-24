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
    public class BrowsingPositionPacket
    {      
        public static double ZOOM_SCALE_FACTOR = 1024;
        [ProtoMember(1)]
        public int atlasID;
        [ProtoMember(2)]
        public int dimension;
        [ProtoMember(3)]
        public int x;
        [ProtoMember(4)]
        public int y;
        [ProtoMember(5)]
        public double zoom;
    }
}
