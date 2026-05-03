using CANAntiqueAtlas.src.core.BiomeDetectors;

namespace CANAntiqueAtlas.Tests;

/// <summary>
/// Tests for BiomeConditions.Fullfills (Stage 2 — block scan condition matching).
/// BiomeConditions only has Fullfills(ClimateCondition, Block, int height, float heightRange).
/// Map-based detection is handled by BiomeDetectorLandform via LandformMap + refinements.
/// </summary>
public class BiomeConditionsTests
{
    // ========== Height range tests ==========

    [Fact]
    public void Fullfills_HeightOutOfRange_ReturnsFalse()
    {
        var cond = CreateBasicCondition();
        cond.MinHeight = 100;
        cond.MaxHeight = 200;

        var climate = CreateDefaultClimate();

        Assert.False(cond.Fullfills(climate, null, 50, 5));  // below min
        Assert.False(cond.Fullfills(climate, null, 250, 5)); // above max
        Assert.True(cond.Fullfills(climate, null, 150, 5));  // in range
    }

    [Fact]
    public void Fullfills_HeightAtExactBoundary_ReturnsTrue()
    {
        var cond = CreateBasicCondition();
        cond.MinHeight = 100;
        cond.MaxHeight = 200;

        var climate = CreateDefaultClimate();

        Assert.True(cond.Fullfills(climate, null, 100, 5));
        Assert.True(cond.Fullfills(climate, null, 200, 5));
    }

    // ========== Temperature tests ==========

    [Fact]
    public void Fullfills_TemperatureBelowMin_ReturnsFalse()
    {
        var cond = CreateBasicCondition();
        cond.MinTemperature = -5;
        cond.MaxTemperature = 25;

        var climate = CreateDefaultClimate();
        climate.Temperature = -10;

        Assert.False(cond.Fullfills(climate, null, 100, 5));
    }

    [Fact]
    public void Fullfills_TemperatureAboveMax_ReturnsFalse()
    {
        var cond = CreateBasicCondition();
        cond.MinTemperature = -5;
        cond.MaxTemperature = 25;

        var climate = CreateDefaultClimate();
        climate.Temperature = 30;

        Assert.False(cond.Fullfills(climate, null, 100, 5));
    }

    [Fact]
    public void Fullfills_TemperatureInRange_ReturnsTrue()
    {
        var cond = CreateBasicCondition();
        cond.MinTemperature = -5;
        cond.MaxTemperature = 25;

        var climate = CreateDefaultClimate();
        climate.Temperature = 10;

        Assert.True(cond.Fullfills(climate, null, 100, 5));
    }

    // ========== Rainfall tests ==========

    [Fact]
    public void Fullfills_RainfallBelowMin_ReturnsFalse()
    {
        var cond = CreateBasicCondition();
        cond.MinRainfall = 0.3;
        cond.MaxRainfall = 1;

        var climate = CreateDefaultClimate();
        climate.Rainfall = 0.1f;

        Assert.False(cond.Fullfills(climate, null, 100, 5));
    }

    [Fact]
    public void Fullfills_RainfallAboveMax_ReturnsFalse()
    {
        var cond = CreateBasicCondition();
        cond.MinRainfall = 0;
        cond.MaxRainfall = 0.5;

        var climate = CreateDefaultClimate();
        climate.Rainfall = 0.8f;

        Assert.False(cond.Fullfills(climate, null, 100, 5));
    }

    // ========== Fertility tests ==========

    [Fact]
    public void Fullfills_FertilityOutOfRange_ReturnsFalse()
    {
        var cond = CreateBasicCondition();
        cond.MinFertility = 0;
        cond.MaxFertility = 0.3;

        var climate = CreateDefaultClimate();
        climate.Fertility = 0.8f;

        Assert.False(cond.Fullfills(climate, null, 100, 5));
    }

    // ========== Forest density tests ==========

    [Fact]
    public void Fullfills_ForestDensityBelowMin_ReturnsFalse()
    {
        var cond = CreateBasicCondition();
        cond.MinForestDensity = 0.4f;
        cond.MaxForestDensity = 1;

        var climate = CreateDefaultClimate();
        climate.ForestDensity = 0.1f;

        Assert.False(cond.Fullfills(climate, null, 100, 5));
    }

    [Fact]
    public void Fullfills_ForestDensityAboveMax_ReturnsFalse()
    {
        var cond = CreateBasicCondition();
        cond.MinForestDensity = 0;
        cond.MaxForestDensity = 0.5f;

        var climate = CreateDefaultClimate();
        climate.ForestDensity = 0.8f;

        Assert.False(cond.Fullfills(climate, null, 100, 5));
    }

    // ========== Shrub density tests ==========

    [Fact]
    public void Fullfills_ShrubDensityOutOfRange_ReturnsFalse()
    {
        var cond = CreateBasicCondition();
        cond.MinShrubDensity = 0.2f;
        cond.MaxShrubDensity = 0.6f;

        var climate = CreateDefaultClimate();
        climate.ShrubDensity = 0.9f;

        Assert.False(cond.Fullfills(climate, null, 100, 5));
    }

    // ========== HeightRange (MinHeightRange / MaxHeightRange) tests ==========

    [Fact]
    public void Fullfills_HeightRangeBelowMin_ReturnsFalse()
    {
        var cond = CreateBasicCondition();
        cond.MinHeightRange = 7;
        cond.MaxHeightRange = 15;

        var climate = CreateDefaultClimate();

        Assert.False(cond.Fullfills(climate, null, 100, 3));
    }

    [Fact]
    public void Fullfills_HeightRangeAboveMax_ReturnsFalse()
    {
        var cond = CreateBasicCondition();
        cond.MinHeightRange = 7;
        cond.MaxHeightRange = 15;

        var climate = CreateDefaultClimate();

        Assert.False(cond.Fullfills(climate, null, 100, 20));
    }

    [Fact]
    public void Fullfills_HeightRangeInRange_ReturnsTrue()
    {
        var cond = CreateBasicCondition();
        cond.MinHeightRange = 7;
        cond.MaxHeightRange = 15;

        var climate = CreateDefaultClimate();

        Assert.True(cond.Fullfills(climate, null, 100, 10));
    }

    [Fact]
    public void Fullfills_HeightRangeDisabled_IgnoresValue()
    {
        // Default MinHeightRange/MaxHeightRange is -1 (disabled)
        var cond = CreateBasicCondition();

        var climate = CreateDefaultClimate();

        Assert.True(cond.Fullfills(climate, null, 100, 999));
        Assert.True(cond.Fullfills(climate, null, 100, 0));
    }

    // ========== Block wildcard tests ==========

    [Fact]
    public void Fullfills_NoBlockWildCards_IgnoresBlock()
    {
        var cond = CreateBasicCondition();
        // No BlockCodeWildCards → block parameter is ignored

        var climate = CreateDefaultClimate();

        Assert.True(cond.Fullfills(climate, null, 100, 5));
    }

    // ========== Combined condition tests ==========

    [Fact]
    public void Fullfills_AllConditionsMet_ReturnsTrue()
    {
        var cond = new BiomeConditions
        {
            MinHeight = 50,
            MaxHeight = 200,
            MinTemperature = -5,
            MaxTemperature = 25,
            MinRainfall = 0.2,
            MaxRainfall = 0.8,
            MinFertility = 0.1,
            MaxFertility = 0.9,
            MinForestDensity = 0.1f,
            MaxForestDensity = 0.9f,
            MinShrubDensity = 0.1f,
            MaxShrubDensity = 0.9f,
            MinHeightRange = 3,
            MaxHeightRange = 20
        };

        var climate = new Vintagestory.API.Common.ClimateCondition
        {
            Temperature = 10,
            Rainfall = 0.5f,
            Fertility = 0.5f,
            ForestDensity = 0.5f,
            ShrubDensity = 0.5f
        };

        Assert.True(cond.Fullfills(climate, null, 100, 10));
    }

    [Fact]
    public void Fullfills_OneConditionFails_ReturnsFalse()
    {
        var cond = new BiomeConditions
        {
            MinHeight = 50,
            MaxHeight = 200,
            MinTemperature = -5,
            MaxTemperature = 25,
            MinRainfall = 0.2,
            MaxRainfall = 0.8,
            MinFertility = 0.1,
            MaxFertility = 0.9,
            MinForestDensity = 0.1f,
            MaxForestDensity = 0.9f,
            MinShrubDensity = 0.1f,
            MaxShrubDensity = 0.9f
        };

        // Everything in range except temperature
        var climate = new Vintagestory.API.Common.ClimateCondition
        {
            Temperature = 30, // above MaxTemperature
            Rainfall = 0.5f,
            Fertility = 0.5f,
            ForestDensity = 0.5f,
            ShrubDensity = 0.5f
        };

        Assert.False(cond.Fullfills(climate, null, 100, 5));
    }

    // ========== Priority comparison ==========

    [Fact]
    public void Priority_HigherPriorityConditionShouldBePreferred()
    {
        var forest = new BiomeConditions { Priority = 1.3f };
        var plains = new BiomeConditions { Priority = 1.0f };

        Assert.True(forest.Priority > plains.Priority);
    }

    // ========== Helpers ==========

    private static BiomeConditions CreateBasicCondition()
    {
        return new BiomeConditions
        {
            MinHeight = 0,
            MaxHeight = 500,
            MinTemperature = -50,
            MaxTemperature = 50,
            MinRainfall = 0,
            MaxRainfall = 1,
            MinFertility = 0,
            MaxFertility = 1,
            MinForestDensity = 0,
            MaxForestDensity = 1,
            MinShrubDensity = 0,
            MaxShrubDensity = 1
        };
    }

    private static Vintagestory.API.Common.ClimateCondition CreateDefaultClimate()
    {
        return new Vintagestory.API.Common.ClimateCondition
        {
            Temperature = 10,
            Rainfall = 0.5f,
            Fertility = 0.5f,
            ForestDensity = 0.5f,
            ShrubDensity = 0.5f
        };
    }
}
