using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.core;
using ProtoBuf;

namespace CANAntiqueAtlas.src.network.client
{
    [ProtoContract]
    public class TileGroupsPacket
    {
        [ProtoMember(1)]
        public int atlasID;
        [ProtoMember(2)]
        public List<TileGroup> tileGroups;

        public static int TILE_GROUPS_PER_PACKET = 100;
    }
}
