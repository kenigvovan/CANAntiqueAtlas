using CANAntiqueAtlas.src.core.BiomeDetectors;

namespace CANAntiqueAtlas.Tests;

/// <summary>
/// Tests for refinement rule matching logic.
/// Replicates BiomeDetectorLandform.ApplyRefinements algorithm to test
/// rule conditions without requiring VS runtime.
/// </summary>
public class RefinementRuleTests
{
    // ========== BaseBiome filtering ==========

    [Fact]
    public void ApplyRefinements_BaseBiomeNotInList_RuleSkipped()
    {
        var rules = new List<RefinementRule>
        {
            new()
            {
                BaseBiomes = new() { "Plains" },
                ResultBiome = "Desert",
                Priority = 1
            }
        };
        var biomeNameToId = new Dictionary<string, int> { { "Desert", 99 } };

        int result = ApplyRefinements(rules, biomeNameToId, "Mountains", 5,
            forestValue: 10, oceanValue: 0, shrubValue: 0,
            temperature: 20, rainfall: 0.1f, normalizedHeight: 0.6f);

        // Rule skipped because base biome is Mountains, not Plains
        Assert.Equal(5, result);
    }

    [Fact]
    public void ApplyRefinements_BaseBiomeMatches_RuleApplied()
    {
        var rules = new List<RefinementRule>
        {
            new()
            {
                BaseBiomes = new() { "Plains" },
                MaxForestMapValue = 20,
                MaxRainfall = 0.3f,
                ResultBiome = "Desert",
                Priority = 2
            }
        };
        var biomeNameToId = new Dictionary<string, int> { { "Desert", 99 } };

        int result = ApplyRefinements(rules, biomeNameToId, "Plains", 3,
            forestValue: 10, oceanValue: 0, shrubValue: 0,
            temperature: 20, rainfall: 0.1f, normalizedHeight: 0.45f);

        Assert.Equal(99, result);
    }

    [Fact]
    public void ApplyRefinements_EmptyBaseBiomes_AppliesToAny()
    {
        var rules = new List<RefinementRule>
        {
            new()
            {
                BaseBiomes = new(), // empty = applies to any base biome
                MinOceanMapValue = 180,
                ResultBiome = "Water",
                Priority = 10
            }
        };
        var biomeNameToId = new Dictionary<string, int> { { "Water", 1 } };

        int result = ApplyRefinements(rules, biomeNameToId, "Mountains", 5,
            forestValue: 50, oceanValue: 200, shrubValue: 30,
            temperature: 10, rainfall: 0.5f, normalizedHeight: 0.6f);

        Assert.Equal(1, result);
    }

    // ========== Forest map value ==========

    [Fact]
    public void ApplyRefinements_ForestBelowMin_RuleSkipped()
    {
        var rules = new List<RefinementRule>
        {
            new()
            {
                MinForestMapValue = 100,
                BaseBiomes = new() { "Plains" },
                ResultBiome = "TemperateForest",
                Priority = 1
            }
        };
        var biomeNameToId = new Dictionary<string, int> { { "TemperateForest", 10 } };

        int result = ApplyRefinements(rules, biomeNameToId, "Plains", 3,
            forestValue: 50, oceanValue: 0, shrubValue: 0,
            temperature: 10, rainfall: 0.5f, normalizedHeight: 0.45f);

        Assert.Equal(3, result); // unchanged
    }

    [Fact]
    public void ApplyRefinements_ForestAboveMax_RuleSkipped()
    {
        var rules = new List<RefinementRule>
        {
            new()
            {
                MaxForestMapValue = 20,
                BaseBiomes = new() { "Plains" },
                ResultBiome = "Desert",
                Priority = 2
            }
        };
        var biomeNameToId = new Dictionary<string, int> { { "Desert", 99 } };

        int result = ApplyRefinements(rules, biomeNameToId, "Plains", 3,
            forestValue: 50, oceanValue: 0, shrubValue: 0,
            temperature: 20, rainfall: 0.1f, normalizedHeight: 0.45f);

        Assert.Equal(3, result); // unchanged — forest too high
    }

    // ========== Ocean map value ==========

    [Fact]
    public void ApplyRefinements_OceanAboveMin_RuleApplied()
    {
        var rules = new List<RefinementRule>
        {
            new()
            {
                MinOceanMapValue = 180,
                ResultBiome = "Water",
                Priority = 10
            }
        };
        var biomeNameToId = new Dictionary<string, int> { { "Water", 1 } };

        int result = ApplyRefinements(rules, biomeNameToId, "Plains", 3,
            forestValue: 0, oceanValue: 200, shrubValue: 0,
            temperature: 10, rainfall: 0.5f, normalizedHeight: 0.45f);

        Assert.Equal(1, result);
    }

    [Fact]
    public void ApplyRefinements_OceanBelowMin_RuleSkipped()
    {
        var rules = new List<RefinementRule>
        {
            new()
            {
                MinOceanMapValue = 180,
                ResultBiome = "Water",
                Priority = 10
            }
        };
        var biomeNameToId = new Dictionary<string, int> { { "Water", 1 } };

        int result = ApplyRefinements(rules, biomeNameToId, "Plains", 3,
            forestValue: 0, oceanValue: 100, shrubValue: 0,
            temperature: 10, rainfall: 0.5f, normalizedHeight: 0.45f);

        Assert.Equal(3, result); // unchanged
    }

    // ========== Temperature ==========

    [Fact]
    public void ApplyRefinements_TemperatureAboveMax_RuleSkipped()
    {
        var rules = new List<RefinementRule>
        {
            new()
            {
                MaxTemperature = -5,
                MinTerrainHeight = 0.625f,
                BaseBiomes = new() { "Mountains" },
                ResultBiome = "MountainsSnowCaps",
                Priority = 3
            }
        };
        var biomeNameToId = new Dictionary<string, int> { { "MountainsSnowCaps", 6 } };

        int result = ApplyRefinements(rules, biomeNameToId, "Mountains", 5,
            forestValue: 0, oceanValue: 0, shrubValue: 0,
            temperature: 10, rainfall: 0.5f, normalizedHeight: 0.7f);

        Assert.Equal(5, result); // too warm for snow caps
    }

    [Fact]
    public void ApplyRefinements_TemperatureBelowMax_RuleApplied()
    {
        var rules = new List<RefinementRule>
        {
            new()
            {
                MaxTemperature = -5,
                MinTerrainHeight = 0.625f,
                BaseBiomes = new() { "Mountains" },
                ResultBiome = "MountainsSnowCaps",
                Priority = 3
            }
        };
        var biomeNameToId = new Dictionary<string, int> { { "MountainsSnowCaps", 6 } };

        int result = ApplyRefinements(rules, biomeNameToId, "Mountains", 5,
            forestValue: 0, oceanValue: 0, shrubValue: 0,
            temperature: -10, rainfall: 0.5f, normalizedHeight: 0.7f);

        Assert.Equal(6, result);
    }

    // ========== Terrain height ==========

    [Fact]
    public void ApplyRefinements_HeightBelowMin_RuleSkipped()
    {
        var rules = new List<RefinementRule>
        {
            new()
            {
                MinTerrainHeight = 0.54f,
                BaseBiomes = new() { "Plains" },
                ResultBiome = "Plateau",
                Priority = 0.3f
            }
        };
        var biomeNameToId = new Dictionary<string, int> { { "Plateau", 4 } };

        int result = ApplyRefinements(rules, biomeNameToId, "Plains", 3,
            forestValue: 50, oceanValue: 0, shrubValue: 0,
            temperature: 10, rainfall: 0.5f, normalizedHeight: 0.45f);

        Assert.Equal(3, result); // too low for plateau
    }

    // ========== Priority ordering ==========

    [Fact]
    public void ApplyRefinements_HigherPriorityWins()
    {
        // Rules sorted by priority descending (as in BiomeDetectorLandform)
        var rules = new List<RefinementRule>
        {
            new()
            {
                MinOceanMapValue = 180,
                ResultBiome = "Water",
                Priority = 10
            },
            new()
            {
                MinForestMapValue = 100,
                BaseBiomes = new() { "Plains" },
                ResultBiome = "TemperateForest",
                Priority = 1
            }
        };
        var biomeNameToId = new Dictionary<string, int>
        {
            { "Water", 1 },
            { "TemperateForest", 10 }
        };

        // Both rules could match (ocean=200, forest=150)
        int result = ApplyRefinements(rules, biomeNameToId, "Plains", 3,
            forestValue: 150, oceanValue: 200, shrubValue: 0,
            temperature: 10, rainfall: 0.5f, normalizedHeight: 0.45f);

        // OceanOverride (prio 10) wins over DenseForest (prio 1)
        Assert.Equal(1, result);
    }

    [Fact]
    public void ApplyRefinements_NoRulesMatch_ReturnsBaseBiomeId()
    {
        var rules = new List<RefinementRule>
        {
            new()
            {
                MinOceanMapValue = 180,
                ResultBiome = "Water",
                Priority = 10
            }
        };
        var biomeNameToId = new Dictionary<string, int> { { "Water", 1 } };

        int result = ApplyRefinements(rules, biomeNameToId, "Plains", 3,
            forestValue: 50, oceanValue: 0, shrubValue: 0,
            temperature: 10, rainfall: 0.5f, normalizedHeight: 0.45f);

        Assert.Equal(3, result);
    }

    [Fact]
    public void ApplyRefinements_EmptyRules_ReturnsBaseBiomeId()
    {
        var rules = new List<RefinementRule>();
        var biomeNameToId = new Dictionary<string, int>();

        int result = ApplyRefinements(rules, biomeNameToId, "Plains", 3,
            forestValue: 50, oceanValue: 0, shrubValue: 0,
            temperature: 10, rainfall: 0.5f, normalizedHeight: 0.45f);

        Assert.Equal(3, result);
    }

    // ========== Disabled checks (value = -1 or NaN) ==========

    [Fact]
    public void ApplyRefinements_DisabledChecks_DoNotBlock()
    {
        var rules = new List<RefinementRule>
        {
            new()
            {
                // Only ResultBiome set, all thresholds at defaults (-1/NaN)
                ResultBiome = "Water",
                Priority = 1
            }
        };
        var biomeNameToId = new Dictionary<string, int> { { "Water", 1 } };

        // Any values should pass since all checks are disabled
        int result = ApplyRefinements(rules, biomeNameToId, "Plains", 3,
            forestValue: 255, oceanValue: 255, shrubValue: 255,
            temperature: 50, rainfall: 0.9f, normalizedHeight: 0.99f);

        Assert.Equal(1, result);
    }

    // ========== Helper: Replicates ApplyRefinements from BiomeDetectorLandform ==========

    private static int ApplyRefinements(
        List<RefinementRule> sortedRefinements,
        Dictionary<string, int> biomeNameToId,
        string baseBiomeName, int baseBiomeId,
        int forestValue, int oceanValue, int shrubValue,
        float temperature, float rainfall, float normalizedHeight)
    {
        foreach (var rule in sortedRefinements)
        {
            if (rule.BaseBiomes.Count > 0 && baseBiomeName != null &&
                !rule.BaseBiomes.Contains(baseBiomeName))
            {
                continue;
            }

            if (rule.MinForestMapValue != -1 && forestValue < rule.MinForestMapValue) continue;
            if (rule.MaxForestMapValue != -1 && forestValue > rule.MaxForestMapValue) continue;
            if (rule.MinOceanMapValue != -1 && oceanValue < rule.MinOceanMapValue) continue;
            if (rule.MaxOceanMapValue != -1 && oceanValue > rule.MaxOceanMapValue) continue;
            if (rule.MinShrubMapValue != -1 && shrubValue < rule.MinShrubMapValue) continue;
            if (rule.MaxShrubMapValue != -1 && shrubValue > rule.MaxShrubMapValue) continue;

            if (!float.IsNaN(rule.MinTemperature) && temperature < rule.MinTemperature) continue;
            if (!float.IsNaN(rule.MaxTemperature) && temperature > rule.MaxTemperature) continue;
            if (!float.IsNaN(rule.MinRainfall) && rainfall < rule.MinRainfall) continue;
            if (!float.IsNaN(rule.MaxRainfall) && rainfall > rule.MaxRainfall) continue;

            if (rule.MinTerrainHeight != -1 && normalizedHeight < rule.MinTerrainHeight) continue;
            if (rule.MaxTerrainHeight != -1 && normalizedHeight > rule.MaxTerrainHeight) continue;

            if (rule.ResultBiome != null && biomeNameToId.TryGetValue(rule.ResultBiome, out int resultId))
            {
                return resultId;
            }
        }

        return baseBiomeId;
    }
}
