using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using static CANAntiqueAtlas.src.core.BiomeDetectors.IBiomeDetector;

namespace CANAntiqueAtlas.src.core.BiomeDetectors
{
    public class BiomeConditions
    {
        public string Name { get; set; }
        public BiomeType BiomeType {  get; set; }
        public bool Fallback { get; set; } = false;
        public int MaxHeight {  get; set; }
        public int MinHeight { get; set; }
        public HashSet<string> BlockCodeWildCards = new();
        public float Priority {  get; set; }
        public double MaxTemperature { get; set; }
        public double MinTemperature { get; set; }
        public double MaxRainfall {  get; set; }
        public double MinRainfall { get; set; }
        public double MaxFertility { get; set; }
        public double MinFertility { get; set; }
        public float MaxForestDensity { get; set; }
        public float MinForestDensity { get; set; }
        public float MaxShrubDensity { get; set; }
        public float MinShrubDensity { get; set; }
        public float MinHeightRange { get; set; } = -1;
        public float MaxHeightRange { get; set; } = -1;
        public BiomeConditions() 
        { 
        }
        public bool Fullfills(ClimateCondition cond, Block block, int height, float heightRange)
        {
            if (block.Code.Path.Contains("redwood"))
            {
                var c = 3;
            }
            //"sludgygravel"
            if ((MinHeightRange != -1 && heightRange < MinHeightRange) || (MaxHeightRange != -1 && heightRange > MaxHeightRange)) return false;
            if (this.MinHeight > height || this.MaxHeight < height) return false;
            if (cond.Temperature < MinTemperature || cond.Temperature > MaxTemperature) return false;
            if (cond.Rainfall < MinRainfall || cond.Rainfall > MaxRainfall) return false;
            if (cond.Fertility < MinFertility || cond.Fertility > MaxFertility) return false;
            if (cond.ForestDensity < MinForestDensity || cond.ForestDensity > MaxForestDensity) return false;
            if (cond.ShrubDensity < MinShrubDensity || cond.ShrubDensity > MaxShrubDensity) return false;
            if (block != null && BlockCodeWildCards.Count > 0)
            {
                if (block.Code.Path.Contains("redwood"))
                {
                    var c = 3;
                }
                bool found = false;
                foreach (var it in BlockCodeWildCards)
                {
                    if(WildcardUtil.Match(it, block.Code.Path))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }
            return true;
        }
    }
}
