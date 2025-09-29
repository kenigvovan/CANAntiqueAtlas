using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.core;
using ProtoBuf;

namespace CANAntiqueAtlas.src.network.server
{
    [ProtoContract]
    public class PlayerJoinedMapDataSeen
    {
        [ProtoMember(1)]
        public AtlasSeenData SeenData;
    }
}
