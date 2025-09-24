using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common.Database;

namespace CANAntiqueAtlas.src.core
{
    public interface IBiomeDetector
    {
        public enum BiomeType
        {
            NOT_FOUND = -1,
            Glacier = 0,
            Tundra = 1,
            Taiga = 2,
            TemperateForest = 3,
            Plains = 4,
            Savanna = 5,
            Desert = 6,
            Rainforest = 7,
            Swamp = 8,
            Water = 9,
        }

        /** Finds the biome ID to be used for a given chunk. */
        BiomeType GetBiomeID(IMapChunk chunk, Vec2i chunkCoords);
    }
}
