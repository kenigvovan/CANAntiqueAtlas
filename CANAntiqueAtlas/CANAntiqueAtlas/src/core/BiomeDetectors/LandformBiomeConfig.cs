using System.Collections.Generic;

namespace CANAntiqueAtlas.src.core.BiomeDetectors
{
    /// <summary>
    /// Configuration model for landform-based biome detection.
    /// Deserialized from landform_mapping.json.
    /// </summary>
    public class LandformBiomeConfig
    {
        /// <summary>
        /// Maps landform code wildcards to biome names.
        /// Key: wildcard pattern (e.g. "ocean*", "mountain*")
        /// Value: biome name matching BiomeType / BiomesMapping (e.g. "Water", "Mountains")
        /// Evaluated in order — first match wins.
        /// </summary>
        public Dictionary<string, string> LandformToBiome { get; set; } = new();

        /// <summary>
        /// Refinement rules applied after the base landform lookup.
        /// Key: rule name (for readability only)
        /// Value: refinement definition
        /// </summary>
        public Dictionary<string, RefinementRule> Refinements { get; set; } = new();
    }

    public class RefinementRule
    {
        /// <summary>Base biome names this refinement can override. Empty = any.</summary>
        public List<string> BaseBiomes { get; set; } = new();

        // Region map value thresholds (0-255 from IntDataMap2D). -1 = don't check.
        public float MinForestMapValue { get; set; } = -1;
        public float MaxForestMapValue { get; set; } = -1;
        public float MinOceanMapValue { get; set; } = -1;
        public float MaxOceanMapValue { get; set; } = -1;
        public float MinShrubMapValue { get; set; } = -1;
        public float MaxShrubMapValue { get; set; } = -1;

        // Climate thresholds. NaN = don't check.
        public float MinTemperature { get; set; } = float.NaN;
        public float MaxTemperature { get; set; } = float.NaN;
        public float MinRainfall { get; set; } = float.NaN;
        public float MaxRainfall { get; set; } = float.NaN;

        // Terrain height (normalized 0-1). -1 = don't check.
        public float MinTerrainHeight { get; set; } = -1;
        public float MaxTerrainHeight { get; set; } = -1;

        /// <summary>The biome name to assign if this refinement matches.</summary>
        public string ResultBiome { get; set; }

        /// <summary>
        /// Priority among refinements. Higher = checked first and wins over lower.
        /// Default 0.
        /// </summary>
        public float Priority { get; set; } = 0;
    }
}
