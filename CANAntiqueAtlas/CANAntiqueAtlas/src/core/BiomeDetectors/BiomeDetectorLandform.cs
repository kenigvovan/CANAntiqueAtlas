using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;
using static CANAntiqueAtlas.src.core.BiomeDetectors.IBiomeDetector;

namespace CANAntiqueAtlas.src.core.BiomeDetectors
{
    /// <summary>
    /// IBiomeDetector implementation that uses LandformMap as the primary biome source (v2).
    /// Stage 1: LandformMap lookup + region map refinements (no block reads).
    /// Stage 2: Block scan for marker biomes (HotSpring, Swamp, Jungle, etc.).
    /// Falls back to BiomeDetectorBase if LandformMap is unavailable.
    /// </summary>
    public class BiomeDetectorLandform : IBiomeDetector
    {
        private readonly ICoreServerAPI sapi;
        private readonly BiomeDetectorBase fallbackDetector;

        // Landform index → biome name (built at startup from NoiseLandforms + config)
        private Dictionary<int, string> landformIndexToBiomeName = new();

        // Resolved: landform index → biome ID
        private Dictionary<int, int> landformIndexToBiomeId = new();

        // Refinement rules sorted by priority descending
        private List<RefinementRule> sortedRefinements = new();

        // Biome name → biome ID (resolved after biomes are registered)
        private Dictionary<string, int> biomeNameToId = new();

        // Block-scan conditions (Stage 2 marker biomes)
        private List<BiomeConditions> blockScanConditions = new();

        private Dictionary<int, float> FinalSelectionPriority = new();

        // 4x4 sample grid offsets for block scanning
        private static readonly int[] SampleOffsets = { 2, 6, 10, 14 };

        private bool landformMapAvailable = false;

        public BiomeDetectorLandform(ICoreServerAPI sapi, BiomeDetectorBase fallbackDetector)
        {
            this.sapi = sapi;
            this.fallbackDetector = fallbackDetector;
        }

        /// <summary>
        /// Initializes landform→biome mapping from NoiseLandforms and config.
        /// Call after all biomes are registered (so BiomesMapping is populated).
        /// </summary>
        public void InitializeLandformMapping(LandformBiomeConfig config)
        {
            // Load NoiseLandforms from the world generator
            var landforms = NoiseLandforms.landforms;
            if (landforms == null || landforms.LandFormsByIndex == null || landforms.LandFormsByIndex.Length == 0)
            {
                sapi.Logger.Warning("[CANAntiqueAtlas] NoiseLandforms not available — BiomeDetectorLandform will use fallback detector.");
                landformMapAvailable = false;
                return;
            }

            // Build landformIndex → biomeName mapping using wildcard matching on landform codes
            var orderedPatterns = config.LandformToBiome.ToList();

            for (int i = 0; i < landforms.LandFormsByIndex.Length; i++)
            {
                var variant = landforms.LandFormsByIndex[i];
                if (variant?.Code == null) continue;

                string landformCode = variant.Code.Path;

                foreach (var kvp in orderedPatterns)
                {
                    if (WildcardUtil.Match(kvp.Key, landformCode))
                    {
                        landformIndexToBiomeName[i] = kvp.Value;
                        break;
                    }
                }
            }

            // Resolve biome names to IDs
            foreach (var kvp in landformIndexToBiomeName)
            {
                if (CANAntiqueAtlas.BiomesMapping.TryGetValue(kvp.Value, out int biomeId))
                {
                    landformIndexToBiomeId[kvp.Key] = biomeId;
                }
                else
                {
                    sapi.Logger.Warning("[CANAntiqueAtlas] Landform biome mapping references unknown biome '{0}' — skipping.", kvp.Value);
                }
            }

            // Resolve refinement biome names to IDs and sort by priority
            sortedRefinements = config.Refinements.Values
                .OrderByDescending(r => r.Priority)
                .ToList();

            foreach (var rule in sortedRefinements)
            {
                if (rule.ResultBiome != null && CANAntiqueAtlas.BiomesMapping.TryGetValue(rule.ResultBiome, out int id))
                {
                    biomeNameToId[rule.ResultBiome] = id;
                }
            }

            landformMapAvailable = landformIndexToBiomeId.Count > 0;

            sapi.Logger.Notification("[CANAntiqueAtlas] BiomeDetectorLandform initialized: {0} landform→biome mappings, {1} refinement rules, available={2}",
                landformIndexToBiomeId.Count, sortedRefinements.Count, landformMapAvailable);
        }

        public void RegisterBiome(BiomeConditions conditions)
        {
            // Always register in fallback detector (through interface — RegisterBiome is explicit)
            ((IBiomeDetector)fallbackDetector).RegisterBiome(conditions);

            // Biomes with BlockCodeWildCards need block scanning (Stage 2)
            if (conditions.BlockCodeWildCards != null && conditions.BlockCodeWildCards.Count > 0)
            {
                blockScanConditions.Add(conditions);
            }
        }

        public void AddToFinalPriorityMap(Dictionary<int, float> finalPriorityMap)
        {
            // Update fallback detector's priority map directly
            foreach (var it in finalPriorityMap)
            {
                fallbackDetector.FinalSelectionPriority[it.Key] = it.Value;
                FinalSelectionPriority[it.Key] = it.Value;
            }
        }

        /// <summary>
        /// Returns landform info string for a given block position (for debug command).
        /// </summary>
        public string GetLandformInfoAtPos(BlockPos pos)
        {
            if (!landformMapAvailable)
                return "LandformMap not available (using fallback detector).";

            var blockAccessor = sapi.World.BlockAccessor;
            IMapChunk mapChunk = blockAccessor.GetMapChunk(pos.X / 32, pos.Z / 32);
            if (mapChunk == null)
                return "Chunk not loaded at this position.";

            IMapRegion region = mapChunk.MapRegion;
            int regionSize = sapi.WorldManager.RegionSize;

            float normX = (float)((double)pos.X / regionSize % 1.0);
            float normZ = (float)((double)pos.Z / regionSize % 1.0);

            int landformIndex = region.LandformMap.GetUnpaddedColorLerpedForNormalizedPos(normX, normZ);

            var landforms = NoiseLandforms.landforms;
            string landformCode = "unknown";
            if (landforms?.LandFormsByIndex != null && landformIndex >= 0 && landformIndex < landforms.LandFormsByIndex.Length)
            {
                var variant = landforms.LandFormsByIndex[landformIndex];
                if (variant?.Code != null)
                    landformCode = variant.Code.Path;
            }

            landformIndexToBiomeName.TryGetValue(landformIndex, out string mappedBiome);
            landformIndexToBiomeId.TryGetValue(landformIndex, out int biomeId);

            // Read region maps for refinement context
            int climateRaw = region.ClimateMap.GetUnpaddedColorLerpedForNormalizedPos(normX, normZ);
            int tempRaw = (climateRaw >> 16) & 255;
            int rainRaw = (climateRaw >> 8) & 255;
            int forestValue = region.ForestMap.GetUnpaddedColorLerpedForNormalizedPos(normX, normZ) & 255;
            int oceanValue = region.OceanMap.GetUnpaddedColorLerpedForNormalizedPos(normX, normZ) & 255;

            // Simulate GetBiomeID for this position
            int tileX = pos.X >> 4;
            int tileZ = pos.Z >> 4;
            var chunkCoords = new Vec2i(tileX / 2, tileZ / 2);
            int actualBiomeId = GetBiomeID(mapChunk, chunkCoords, tileX, tileZ);
            string actualBiomeName = "unknown";
            foreach (var kv in CANAntiqueAtlas.BiomesMapping)
            {
                if (kv.Value == actualBiomeId) { actualBiomeName = kv.Key; break; }
            }

            // Check what's actually stored in the atlas tile
            var mapData = CANAntiqueAtlas.ServerMapInfoData.GetDimensionData();
            var storedTile = mapData.GetTile(tileX, tileZ);
            string storedInfo = storedTile != null
                ? $"biomeId={storedTile.biomeID}"
                : "null";

            string result = $"Landform: \"{landformCode}\" (index {landformIndex})\n" +
                            $"Base biome: {mappedBiome ?? "NOT MAPPED"} (id {biomeId})\n" +
                            $"Final biome: {actualBiomeName} (id {actualBiomeId})\n" +
                            $"Stored tile: {storedInfo}\n" +
                            $"Forest: {forestValue}, Ocean: {oceanValue}, Temp: {tempRaw}, Rain: {rainRaw}";

            return result;
        }

        public int GetBiomeID(IMapChunk chunk, Vec2i chunkCoords, int x, int z)
        {
            if (!landformMapAvailable)
            {
                return fallbackDetector.GetBiomeID(chunk, chunkCoords, x, z);
            }

            IMapRegion region = chunk.MapRegion;
            int regionSize = sapi.WorldManager.RegionSize;
            int mapSizeY = sapi.World.BlockAccessor.MapSizeY;

            int blockCenterX = x * 16 + 8;
            int blockCenterZ = z * 16 + 8;

            float normX = (float)((double)blockCenterX / regionSize % 1.0);
            float normZ = (float)((double)blockCenterZ / regionSize % 1.0);

            // ---- Stage 1: LandformMap lookup ----
            int landformIndex = region.LandformMap.GetUnpaddedColorLerpedForNormalizedPos(normX, normZ);

            int baseBiomeId = -1;
            string baseBiomeName = null;

            if (landformIndexToBiomeId.TryGetValue(landformIndex, out int mappedId))
            {
                baseBiomeId = mappedId;
                landformIndexToBiomeName.TryGetValue(landformIndex, out baseBiomeName);
            }

            // If landform not mapped, try fallback detector
            if (baseBiomeId == -1)
            {
                return fallbackDetector.GetBiomeID(chunk, chunkCoords, x, z);
            }

            // ---- Read region map data for refinements ----
            int climateRaw = region.ClimateMap.GetUnpaddedColorLerpedForNormalizedPos(normX, normZ);
            int tempRaw = (climateRaw >> 16) & 255;
            int rainRaw = (climateRaw >> 8) & 255;

            int forestValue = region.ForestMap.GetUnpaddedColorLerpedForNormalizedPos(normX, normZ) & 255;
            int oceanValue = region.OceanMap.GetUnpaddedColorLerpedForNormalizedPos(normX, normZ) & 255;
            int shrubValue = region.ShrubMap.GetUnpaddedColorLerpedForNormalizedPos(normX, normZ) & 255;

            // Calculate average terrain height
            float avgTerrainHeight = 0;
            int sampleCount = 0;
            ushort[] terrainHeightMap = chunk.WorldGenTerrainHeightMap;

            if (terrainHeightMap != null && terrainHeightMap.Length > 0)
            {
                int offsetX = (x * 16) % 32;
                int offsetZ = (z * 16) % 32;
                int[] subSamples = { 4, 12 };
                foreach (int sx in subSamples)
                {
                    foreach (int sz in subSamples)
                    {
                        int idx = (offsetZ + sz) * 32 + (offsetX + sx);
                        if (idx >= 0 && idx < terrainHeightMap.Length)
                        {
                            avgTerrainHeight += terrainHeightMap[idx];
                            sampleCount++;
                        }
                    }
                }
                if (sampleCount > 0) avgTerrainHeight /= sampleCount;
            }

            float normalizedHeight = avgTerrainHeight / mapSizeY;

            int distToSealevel = (int)avgTerrainHeight - Climate.Sealevel;
            float temperature = Climate.GetScaledAdjustedTemperatureFloat(tempRaw, distToSealevel);
            float rainfall = (float)Climate.GetRainFall(rainRaw, (int)avgTerrainHeight) / 255f;

            // ---- Apply refinements ----
            int refinedBiomeId = ApplyRefinements(baseBiomeName, baseBiomeId,
                forestValue, oceanValue, shrubValue, temperature, rainfall, normalizedHeight);
            //return refinedBiomeId;
            // ---- Stage 2: Block scan for marker biomes ----
            BiomeConditions bestBlockBiome = null;
            if (blockScanConditions.Count > 0)
            {
                int minH = int.MaxValue, maxH = int.MinValue;
                if (terrainHeightMap != null && terrainHeightMap.Length > 0)
                {
                    int offsetX = (x * 16) % 32;
                    int offsetZ = (z * 16) % 32;
                    foreach (int sx in new[] { 4, 12 })
                    {
                        foreach (int sz in new[] { 4, 12 })
                        {
                            int idx = (offsetZ + sz) * 32 + (offsetX + sx);
                            if (idx >= 0 && idx < terrainHeightMap.Length)
                            {
                                int h = terrainHeightMap[idx];
                                if (h < minH) minH = h;
                                if (h > maxH) maxH = h;
                            }
                        }
                    }
                }
                int heightRange = (minH != int.MaxValue) ? (maxH - minH) : 0;
                bestBlockBiome = ScanBlocksForMarkerBiomes(chunk, chunkCoords, x, z, heightRange);
            }

            // ---- Final selection ----
            int finalBiomeId = refinedBiomeId;
            string decisionPath = "refined";
            float bestBlockPrio = 0f, refinedPrio = 0f;
            if (bestBlockBiome != null)
            {
                FinalSelectionPriority.TryGetValue(bestBlockBiome.BiomeId, out bestBlockPrio);
                FinalSelectionPriority.TryGetValue(refinedBiomeId, out refinedPrio);

                if (bestBlockPrio > refinedPrio)
                {
                    finalBiomeId = bestBlockBiome.BiomeId;
                    decisionPath = "blockscan";
                }
            }

            if (CANAntiqueAtlas.config.debugMode)
            {
                CANAntiqueAtlas.BiomesMapping.TryGetValue("Water", out int waterId);
                if (finalBiomeId == waterId && waterId != 0)
                {
                    string blockBiomeName = bestBlockBiome != null
                        ? blockScanConditions.FirstOrDefault(c => c.BiomeId == bestBlockBiome.BiomeId)?.BiomeName ?? "?"
                        : "none";
                    string landformCode = "?";
                    var lfArr = NoiseLandforms.landforms?.LandFormsByIndex;
                    if (lfArr != null && landformIndex >= 0 && landformIndex < lfArr.Length)
                    {
                        landformCode = lfArr[landformIndex]?.Code?.Path ?? "?";
                    }
                    sapi.Logger.Notification(
                        "[CAN][Water-debug] tile=({0},{1}) chunk=({2},{3}) path={4} landform=#{5}({6}=>{7}) refined=#{8}(prio={9:0.##}) blockscan={10}(prio={11:0.##}) ocean={12} forest={13} normH={14:0.###}",
                        x, z, chunkCoords.X, chunkCoords.Y, decisionPath,
                        landformIndex, landformCode, baseBiomeName ?? "?",
                        refinedBiomeId, refinedPrio,
                        blockBiomeName, bestBlockPrio,
                        oceanValue, forestValue, normalizedHeight);
                }
            }

            return finalBiomeId;
        }

        private int ApplyRefinements(string baseBiomeName, int baseBiomeId,
            int forestValue, int oceanValue, int shrubValue,
            float temperature, float rainfall, float normalizedHeight)
        {
            foreach (var rule in sortedRefinements)
            {
                // Check if this refinement applies to the base biome
                if (rule.BaseBiomes.Count > 0 && baseBiomeName != null &&
                    !rule.BaseBiomes.Contains(baseBiomeName))
                {
                    continue;
                }

                // Check region map thresholds
                if (rule.MinForestMapValue != -1 && forestValue < rule.MinForestMapValue) continue;
                if (rule.MaxForestMapValue != -1 && forestValue > rule.MaxForestMapValue) continue;
                if (rule.MinOceanMapValue != -1 && oceanValue < rule.MinOceanMapValue) continue;
                if (rule.MaxOceanMapValue != -1 && oceanValue > rule.MaxOceanMapValue) continue;
                if (rule.MinShrubMapValue != -1 && shrubValue < rule.MinShrubMapValue) continue;
                if (rule.MaxShrubMapValue != -1 && shrubValue > rule.MaxShrubMapValue) continue;

                // Check climate thresholds
                if (!float.IsNaN(rule.MinTemperature) && temperature < rule.MinTemperature) continue;
                if (!float.IsNaN(rule.MaxTemperature) && temperature > rule.MaxTemperature) continue;
                if (!float.IsNaN(rule.MinRainfall) && rainfall < rule.MinRainfall) continue;
                if (!float.IsNaN(rule.MaxRainfall) && rainfall > rule.MaxRainfall) continue;

                // Check terrain height
                if (rule.MinTerrainHeight != -1 && normalizedHeight < rule.MinTerrainHeight) continue;
                if (rule.MaxTerrainHeight != -1 && normalizedHeight > rule.MaxTerrainHeight) continue;

                // All conditions met — apply refinement
                if (rule.ResultBiome != null && biomeNameToId.TryGetValue(rule.ResultBiome, out int resultId))
                {
                    return resultId;
                }
            }

            return baseBiomeId;
        }

        private BiomeConditions ScanBlocksForMarkerBiomes(IMapChunk chunk, Vec2i chunkCoords, int x, int z, int heightRange)
        {
            ClimateCondition cond = sapi.World.BlockAccessor.GetClimateAt(
                new BlockPos(x * 16 + 8, 100, z * 16 + 8), EnumGetClimateMode.WorldGenValues);

            if (cond == null) return null;

            Dictionary<int, int> biomeVotes = new();
            var blockAccessor = sapi.World.BlockAccessor;
            bool debug = CANAntiqueAtlas.config.debugMode;
            Dictionary<string, int> sampledBlocks = debug ? new() : null;

            foreach (int sx in SampleOffsets)
            {
                foreach (int sz in SampleOffsets)
                {
                    var blockCoords = new BlockPos(x * 16 + sx, Climate.Sealevel, z * 16 + sz);
                    int rainHeight = blockAccessor.GetRainMapHeightAt(blockCoords);
                    blockCoords.Y = rainHeight;

                    Block block = blockAccessor.GetBlock(blockCoords);

                    // Water blocks are rain-permeable, so rain height points to the solid floor
                    // under the water. Check one block above to detect water bodies.
                    var blockAbove = blockAccessor.GetBlock(blockCoords.UpCopy());
                    if (blockAbove.Code != null &&
                        (blockAbove.Code.Path.StartsWith("water") || blockAbove.Code.Path.StartsWith("saltwater")))
                    {
                        block = blockAbove;
                        rainHeight = rainHeight + 1;
                    }

                    // Traverse down through leaves/logs/air to find ground
                    if (block.Code != null &&
                        (block.Code.Path.Contains("leaves") || block.Code.Path.Contains("log") || block.Code.Path.Contains("air")))
                    {
                        var tmpCoords = blockCoords.Copy();
                        Block tmpBlock = block;
                        int maxDown = 20;
                        while (maxDown > 0 &&
                            (tmpBlock.Code.Path.Contains("leaves") || tmpBlock.Code.Path.Contains("log") || tmpBlock.Code.Path.Contains("air")))
                        {
                            tmpCoords.Y -= 1;
                            tmpBlock = blockAccessor.GetBlock(tmpCoords);
                            maxDown--;
                        }
                        block = tmpBlock;
                        rainHeight = tmpCoords.Y;
                    }

                    if (debug && block.Code != null)
                    {
                        string p = block.Code.Path;
                        sampledBlocks[p] = sampledBlocks.TryGetValue(p, out var bc) ? bc + 1 : 1;
                    }

                    BiomeConditions bestForPoint = null;
                    foreach (var it in blockScanConditions)
                    {
                        if (it.Fullfills(cond, block, rainHeight, heightRange))
                        {
                            if (bestForPoint == null || it.Priority > bestForPoint.Priority)
                            {
                                bestForPoint = it;
                            }
                        }
                    }

                    if (bestForPoint != null)
                    {
                        biomeVotes.TryGetValue(bestForPoint.BiomeId, out int count);
                        biomeVotes[bestForPoint.BiomeId] = count + 1;
                    }
                }
            }

            if (biomeVotes.Count == 0) return null;

            // Require a marker biome to cover a meaningful portion of the chunk
            // before it can override the landform-based biome. Otherwise a single
            // water/swamp block in mountains/desert would flip the whole tile.
            int totalSamples = SampleOffsets.Length * SampleOffsets.Length;
            int minVotes = Math.Max(2, totalSamples / 4);

            int bestId = -1;
            int bestVotes = 0;
            float bestFinalPrio = -1f;

            foreach (var kvp in biomeVotes)
            {
                if (kvp.Value < minVotes) continue;

                FinalSelectionPriority.TryGetValue(kvp.Key, out float prio);

                bool isBetter = false;
                if (prio > bestFinalPrio)
                    isBetter = true;
                else if (prio == bestFinalPrio && kvp.Value > bestVotes)
                    isBetter = true;
                else if (prio == bestFinalPrio && kvp.Value == bestVotes && (bestId == -1 || kvp.Key < bestId))
                    isBetter = true;

                if (isBetter)
                {
                    bestId = kvp.Key;
                    bestVotes = kvp.Value;
                    bestFinalPrio = prio;
                }
            }

            BiomeConditions winner = (bestId == -1) ? null : blockScanConditions.FirstOrDefault(c => c.BiomeId == bestId);

            if (debug)
            {
                CANAntiqueAtlas.BiomesMapping.TryGetValue("Water", out int waterId);
                bool waterRelated = (winner != null && winner.BiomeId == waterId)
                    || biomeVotes.ContainsKey(waterId);
                if (waterRelated && waterId != 0)
                {
                    var votesStr = string.Join(",", biomeVotes.Select(kv =>
                        (blockScanConditions.FirstOrDefault(c => c.BiomeId == kv.Key)?.BiomeName ?? kv.Key.ToString()) + "=" + kv.Value));
                    var blocksStr = string.Join(",", sampledBlocks.OrderByDescending(kv => kv.Value).Take(8).Select(kv => kv.Key + "=" + kv.Value));
                    sapi.Logger.Notification(
                        "[CAN][Water-scan] tile=({0},{1}) minVotes={2}/{3} winner={4} votes=[{5}] blocks=[{6}]",
                        x, z, minVotes, totalSamples,
                        winner?.BiomeName ?? "none",
                        votesStr, blocksStr);
                }
            }

            return winner;
        }
    }
}
