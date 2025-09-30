using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Vintagestory.API.Common;
using Vintagestory.Common.Database;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;
using static CANAntiqueAtlas.src.core.IBiomeDetector;

namespace CANAntiqueAtlas.src.core
{
    public class BiomeDetectorBase: IBiomeDetector
    {
        /** Increment the counter for water biomes by this much during iteration.
         * This is done so that water pools are more visible. */
        private static int priorityWaterPool = 3, prioritylavaPool = 6;

        /** If no valid biome ID is found, returns {@link IBiomeDetector#NOT_FOUND}. */
        public BiomeType GetBiomeID(IMapChunk chunk, Vec2i chunkCoords)
        {
            int sizeX = 16; // обычно 32
            int sizeZ = 16;

            Dictionary<BiomeType, int> biomeOccurrences = new();

            int stepX = sizeX / 4;
            int stepZ = sizeZ / 4;
            var yMax = chunk.YMax;
            ClimateCondition cond = CANAntiqueAtlas.sapi.World.BlockAccessor.GetClimateAt(new BlockPos(chunkCoords.X * 32, 100, chunkCoords.Y * 32), EnumGetClimateMode.WorldGenValues);
            var blockCoords = new BlockPos(chunkCoords.X * 32 + stepX, yMax, chunkCoords.Y * 32 + stepZ);
            for (int sx = 0; sx < 4; sx++)
            {
                for (int sz = 0; sz < 4; sz++)
                {
                    blockCoords = new BlockPos(chunkCoords.X * 32 + sx * stepX, yMax, chunkCoords.Y * 32 + sz * stepZ);
                    var block = CANAntiqueAtlas.sapi.World.BlockAccessor.GetBlock(blockCoords);
                    if(block.IsLiquid() && block.LiquidCode == "water")
                    {
                        if(biomeOccurrences.TryGetValue(BiomeType.Water, out var curVal))
                        {
                            biomeOccurrences[BiomeType.Water] = curVal + priorityWaterPool;
                        }
                        else
                        {
                            biomeOccurrences[BiomeType.Water] = priorityWaterPool;
                        }
                    }

                    BiomeType biomeId = ClassifyBiomeID(cond);

                    if (!biomeOccurrences.ContainsKey(biomeId))
                    {
                        biomeOccurrences[biomeId] = 1;
                    }
                    else
                    {
                        biomeOccurrences[biomeId]++;
                    }
                }
            }

            // выбрать доминирующий биом
            BiomeType meanBiomeId = BiomeType.NOT_FOUND;
            int meanBiomeOccurences = 0;

            foreach (var kvp in biomeOccurrences)
            {
                if (kvp.Value > meanBiomeOccurences)
                {
                    meanBiomeId = kvp.Key;
                    meanBiomeOccurences = kvp.Value;
                }
            }

            return meanBiomeId;
        }
        private BiomeType ClassifyBiomeID(ClimateCondition cond)
        {
            double temp = cond.Temperature;   // °C
            double rainfall = cond.Rainfall;  // 0..1
            double fertility = cond.Fertility; // 0..1, можно использовать для болот


            if (temp < -5)
                return BiomeType.Glacier;
            if (temp < 2)
            {
                if (rainfall < 0.3) return BiomeType.Tundra; // Tundra
                return BiomeType.Taiga;                    // Taiga
            }

            if (temp < 10)
            {
                if (rainfall < 0.4) return BiomeType.Plains; // Plains / Grassland
                return BiomeType.TemperateForest;                    // Temperate Forest
            }

            if (temp < 20)
            {
                if (rainfall < 0.3) return BiomeType.Savanna; // Savanna
                if (rainfall > 0.7 && fertility > 0.5) return BiomeType.Swamp; // Swamp / Marsh
                return BiomeType.Rainforest;                     // Rainforest / Jungle
            }
            if (rainfall < 0.2) return BiomeType.Desert;     // Desert
            if (rainfall > 0.7 && fertility > 0.5) return BiomeType.Swamp; // Swamp / Marsh
            return BiomeType.Rainforest;                         // Rainforest / Jungle
        }
    }
}
