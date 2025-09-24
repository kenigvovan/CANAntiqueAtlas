using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.core;
using static HarmonyLib.Code;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Vintagestory.Client.NoObf.ClientPlatformWindows;
using Vintagestory.API.Common;
using ProtoBuf;
using Vintagestory.API.Datastructures;

namespace CANAntiqueAtlas.src.network.client
{
    /**
     * Used to sync atlas data from server to client.
     */
    [ProtoContract]
    public class MapDataPacket
    {
        [ProtoMember(1)]
        public int atlasID;
        [ProtoMember(2)]
        public AtlasData data;
    }
}
