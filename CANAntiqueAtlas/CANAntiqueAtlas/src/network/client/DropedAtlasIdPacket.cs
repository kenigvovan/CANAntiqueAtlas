using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace CANAntiqueAtlas.src.network.client
{
    [ProtoContract]
    public class DropedAtlasIdPacket
    {
        [ProtoMember(1)]
        public long atlasID;
    }
}
