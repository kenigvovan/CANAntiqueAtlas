using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.core;
using ProtoBuf;
using Vintagestory.API.MathTools;

namespace CANAntiqueAtlas.src.network.server
{
    [ProtoContract]
    public class NewlySeenChunksAndMapData
    {
        [ProtoMember(1)]
        public Dictionary<long, HashSet<Vec2i>> NewlySeenChunk;
        [ProtoMember(2)]
        public Dictionary<long, HashSet<(int, int, Tile)>> NewMapTiles;
    }
}
