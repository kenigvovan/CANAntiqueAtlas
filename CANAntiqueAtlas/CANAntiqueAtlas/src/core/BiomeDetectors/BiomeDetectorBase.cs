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
using static CANAntiqueAtlas.src.core.BiomeDetectors.IBiomeDetector;
using Vintagestory.API.Datastructures;

namespace CANAntiqueAtlas.src.core.BiomeDetectors
{
    public class BiomeDetectorBase: IBiomeDetector
    {
        /** Increment the counter for water biomes by this much during iteration.
         * This is done so that water pools are more visible. */
        private static int priorityWaterPool = 3, prioritylavaPool = 6;

        public HashSet<BiomeConditions> BiomeConditionsSet = new();
        public Dictionary<BiomeType, float> FinalSelectionPriority = new() { 
            { BiomeType.HotSpring, 1 },
            { BiomeType.Redwood, 0.99f },
            { BiomeType.Jungle, 0.99f } };


        /** If no valid biome ID is found, returns {@link IBiomeDetector#NOT_FOUND}. */
        public BiomeType GetBiomeID(IMapChunk chunk, Vec2i chunkCoords, int x, int z)
        {
            int sizeX = 32;
            int sizeZ = 32;
            Dictionary<BiomeType, int> biomeOccurrences = new();

            int stepX = sizeX / 16;
            int stepZ = sizeZ / 16;
            var yMax = chunk.YMax;
            var p = chunk.RainHeightMap;
            ClimateCondition cond = CANAntiqueAtlas.sapi.World.BlockAccessor.GetClimateAt(new BlockPos(chunkCoords.X * 32, 100, chunkCoords.Y * 32), EnumGetClimateMode.WorldGenValues);
            var blockCoords = new BlockPos(x * 16 + stepX, yMax, z * 16 + stepZ);
            Block[] blockArray = new Block[16 * 16];
            int[] heightArrray = new int[16 * 16];


            for (int sx = 0; sx < 16; sx++)
            {
                for (int sz = 0; sz < 16; sz++)
                {
                    blockCoords = new BlockPos(x * 16 + sx * stepX, 110, z * 16 + sz * stepZ);
                    var rainPoint = (blockCoords.Z % 32) * 32 + (blockCoords.X % 32);
                    var ch = CANAntiqueAtlas.sapi.World.BlockAccessor.GetRainMapHeightAt(blockCoords);
                    
                    blockCoords.Y = ch;
                    var tmpBlock = CANAntiqueAtlas.sapi.World.BlockAccessor.GetBlock(blockCoords);
                    
                    Block block = tmpBlock;
                    int i = 0;
                    var tmpCoords = blockCoords.Copy();
                    if ((tmpBlock.Code.Path.Contains("leaves") || tmpBlock.Code.Path.Contains("log") || tmpBlock.Code.Path.Contains("air")))
                    {
                        while (i < 50 && (tmpBlock.Code.Path.Contains("leaves") || tmpBlock.Code.Path.Contains("log") || tmpBlock.Code.Path.Contains("air")))
                        {
                            tmpCoords.Y -= 1;
                            tmpBlock = CANAntiqueAtlas.sapi.World.BlockAccessor.GetBlock(tmpCoords);
                            i++;
                            if (tmpBlock.Code.Path.Contains("air"))
                            {
                                block = tmpBlock;
                            }
                        }

                    }
                    blockArray[sx * 16 + sz] = block;
                    heightArrray[sx * 16 + sz] = blockCoords.Y;
                }
            }
            int minHeight = heightArrray.Min();
            int maxHeight = heightArrray.Max();
            double avgHeight = heightArrray.Average();
            int range = maxHeight - minHeight;
            var groups = heightArrray
                .GroupBy(h => h / 5)
                .Select(g => new { Range = g.Key * 5, Count = g.Count() })
                .OrderBy(g => g.Range)
                .ToList();
            int mainHeightRange = 0;
            if (groups.Count > 0)
            {
                int minGroupHeight = groups.First().Range;
                int maxGroupHeight = groups.Last().Range;
                mainHeightRange = maxGroupHeight - minGroupHeight;
            }
            Dictionary<string, int> amountBlocks = new();
            foreach(var it in blockArray)
            {
                if(amountBlocks.TryGetValue(it.Code.Path, out var v))
                {
                    amountBlocks[it.Code.Path] = ++v;
                }
                else
                {
                    amountBlocks[it.Code.Path] = 1;
                }
            }
            foreach(var it in amountBlocks)
            {
                Console.WriteLine(string.Format("{0}: {1}", it.Key, it.Value));
            }

            for(int i = 0; i < 16; i++)
                for (int j = 0; j < 16; j++) 
                {
                    BiomeConditions bestSelection = null;
                    //Console.WriteLine(blockArray[i * 16 + j].Code.Path);
                    foreach (var it in this.BiomeConditionsSet)
                    {
                        if (it.Fullfills(cond, blockArray[i * 16 + j], heightArrray[i * 16 + j], mainHeightRange))
                        {
                            if (bestSelection == null || it.Priority > bestSelection.Priority)
                            {
                                bestSelection = it;
                            }
                        }
                    }
                    if (bestSelection != null)
                    {
                        if (!biomeOccurrences.ContainsKey(bestSelection.BiomeType))
                        {
                            biomeOccurrences[bestSelection.BiomeType] = 1;
                        }
                        else
                        {
                            biomeOccurrences[bestSelection.BiomeType]++;
                        }
                    }
                }

            StringBuilder stringBuilder = new StringBuilder();
            foreach (var kvp in biomeOccurrences)
            {
                stringBuilder.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            stringBuilder.AppendLine(string.Format("temp: {0}, rain: {1}, fert: {2}, forest: {3}, shrub: {4}", cond.Temperature, cond.Rainfall, cond.Fertility, cond.ForestDensity, cond.ShrubDensity));

            Console.WriteLine(stringBuilder.ToString());    

            BiomeType meanBiomeId = BiomeType.NOT_FOUND;
            int meanBiomeOccurences = 0;
            float oldSelectionPrioValue = 0;
            if(biomeOccurrences.ContainsKey(BiomeType.Redwood))
            {
                var c = 3;
            }
            foreach (var kvp in biomeOccurrences)
            {
                if(!FinalSelectionPriority.TryGetValue(kvp.Key, out float newValue))
                {
                    newValue = 0;
                }
                if(newValue != 0)
                {
                    if(oldSelectionPrioValue < newValue)
                    {
                        oldSelectionPrioValue = newValue;
                        meanBiomeId = kvp.Key;
                        meanBiomeOccurences = kvp.Value;
                        continue;
                    }
                }

                if (kvp.Value > meanBiomeOccurences && (newValue > oldSelectionPrioValue || oldSelectionPrioValue == 0))
                {
                    oldSelectionPrioValue = newValue;
                    meanBiomeId = kvp.Key;
                    meanBiomeOccurences = kvp.Value;
                }
            }
            if(meanBiomeId == BiomeType.NOT_FOUND)
            {
                meanBiomeId = BiomeType.Plains;
            }

            return meanBiomeId;
        }
        void RegisterBiome(BiomeConditions conditions)
        {
            this.BiomeConditionsSet.Add(conditions);
        }
        void IBiomeDetector.RegisterBiome(BiomeConditions conditions)
        {
            RegisterBiome(conditions);
        }
    }
}
