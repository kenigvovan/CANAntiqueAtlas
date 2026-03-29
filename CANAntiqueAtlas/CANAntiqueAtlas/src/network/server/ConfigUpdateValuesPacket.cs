using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace CANAntiqueAtlas.src.network.server
{
    [ProtoContract]
    public class ConfigUpdateValuesPacket
    {
        [ProtoMember(1)]
        public bool showPlayerMarker;
    }
}
