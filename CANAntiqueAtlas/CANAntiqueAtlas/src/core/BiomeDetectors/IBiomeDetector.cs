using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common.Database;

namespace CANAntiqueAtlas.src.core.BiomeDetectors
{
    public interface IBiomeDetector
    {
        public enum BiomeType
        {
            NOT_FOUND = -1,
            Water = 0,
            TemperateForest = 1,
            Desert = 2,
            Plains = 3,
            Plateau = 4,
            Mountains = 5,
            MountainsSnowCaps = 6,
            SparseForest = 7,
            Hills = 8,
            HotSpring = 9,
            Swamp = 10,
            Redwood = 11,
            Jungle = 12,


            Glacier = 99
            /*Glacier = 0,
            Tundra = 1,
            Taiga = 2,
            TemperateForest = 3,
            Plains = 4,
            Savanna = 5,
            Desert = 6,
            Rainforest = 7,
            Swamp = 8,
            Water = 9,*/
        }
        //public HashSet<BiomeConditions> BiomeConditionsSet { get; set; }
        /** Finds the biome ID to be used for a given chunk. */
        int GetBiomeID(IMapChunk chunk, Vec2i chunkCoords, int x, int z);
        void RegisterBiome(BiomeConditions conditions);
        void AddToFinalPriorityMap(Dictionary<int, float> finalPriorityMap);
    }
}
