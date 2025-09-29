using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.core;
using CANAntiqueAtlas.src.util;
using ProtoBuf;

namespace CANAntiqueAtlas.src.network.server
{
    [ProtoContract]
    public class PlayerJoinedMapData
    {
        [ProtoMember(1)]
        public Dictionary<ShortVec2, Tile> ServerMapInfoData;
    }
}
