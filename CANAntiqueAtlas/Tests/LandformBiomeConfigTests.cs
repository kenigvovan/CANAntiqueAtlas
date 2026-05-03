using CANAntiqueAtlas.src.core.BiomeDetectors;
using Newtonsoft.Json;

namespace CANAntiqueAtlas.Tests;

/// <summary>
/// Tests for LandformBiomeConfig and RefinementRule deserialization from JSON.
/// </summary>
public class LandformBiomeConfigTests
{
    private const string SampleJson = """
    {
      "LandformToBiome": {
        "largeislands": "Water",
        "realisticmountains": "Mountains",
        "plains": "Plains",
        "rollinghills": "Hills"
      },
      "Refinements": {
        "OceanOverride": {
          "MinOceanMapValue": 180,
          "ResultBiome": "Water",
          "Priority": 10
        },
        "DenseForest": {
          "MinForestMapValue": 100,
          "BaseBiomes": ["Plains", "Hills", "Plateau"],
          "ResultBiome": "TemperateForest",
          "Priority": 1
        },
        "Desert": {
          "MaxForestMapValue": 20,
          "MaxRainfall": 0.3,
          "BaseBiomes": ["Plains"],
          "ResultBiome": "Desert",
          "Priority": 2
        },
        "SnowCaps": {
          "MinTerrainHeight": 0.625,
          "MaxTemperature": -5,
          "BaseBiomes": ["Mountains"],
          "ResultBiome": "MountainsSnowCaps",
          "Priority": 3
        }
      }
    }
    """;

    [Fact]
    public void Deserialize_LandformToBiome_HasCorrectEntries()
    {
        var config = JsonConvert.DeserializeObject<LandformBiomeConfig>(SampleJson)!;

        Assert.Equal(4, config.LandformToBiome.Count);
        Assert.Equal("Water", config.LandformToBiome["largeislands"]);
        Assert.Equal("Mountains", config.LandformToBiome["realisticmountains"]);
        Assert.Equal("Plains", config.LandformToBiome["plains"]);
        Assert.Equal("Hills", config.LandformToBiome["rollinghills"]);
    }

    [Fact]
    public void Deserialize_Refinements_HasCorrectCount()
    {
        var config = JsonConvert.DeserializeObject<LandformBiomeConfig>(SampleJson)!;

        Assert.Equal(4, config.Refinements.Count);
        Assert.True(config.Refinements.ContainsKey("OceanOverride"));
        Assert.True(config.Refinements.ContainsKey("DenseForest"));
        Assert.True(config.Refinements.ContainsKey("Desert"));
        Assert.True(config.Refinements.ContainsKey("SnowCaps"));
    }

    [Fact]
    public void Deserialize_OceanOverrideRule_HasCorrectValues()
    {
        var config = JsonConvert.DeserializeObject<LandformBiomeConfig>(SampleJson)!;
        var rule = config.Refinements["OceanOverride"];

        Assert.Equal(180, rule.MinOceanMapValue);
        Assert.Equal("Water", rule.ResultBiome);
        Assert.Equal(10, rule.Priority);
        Assert.Empty(rule.BaseBiomes); // No BaseBiomes = applies to any
    }

    [Fact]
    public void Deserialize_DenseForestRule_HasBaseBiomes()
    {
        var config = JsonConvert.DeserializeObject<LandformBiomeConfig>(SampleJson)!;
        var rule = config.Refinements["DenseForest"];

        Assert.Equal(3, rule.BaseBiomes.Count);
        Assert.Contains("Plains", rule.BaseBiomes);
        Assert.Contains("Hills", rule.BaseBiomes);
        Assert.Contains("Plateau", rule.BaseBiomes);
        Assert.Equal(100, rule.MinForestMapValue);
        Assert.Equal("TemperateForest", rule.ResultBiome);
    }

    [Fact]
    public void Deserialize_DesertRule_HasClimateThresholds()
    {
        var config = JsonConvert.DeserializeObject<LandformBiomeConfig>(SampleJson)!;
        var rule = config.Refinements["Desert"];

        Assert.Equal(20, rule.MaxForestMapValue);
        Assert.Equal(0.3f, rule.MaxRainfall);
        Assert.Equal("Desert", rule.ResultBiome);
        Assert.Equal(2, rule.Priority);
    }

    [Fact]
    public void Deserialize_SnowCapsRule_HasTerrainAndTemperature()
    {
        var config = JsonConvert.DeserializeObject<LandformBiomeConfig>(SampleJson)!;
        var rule = config.Refinements["SnowCaps"];

        Assert.Equal(0.625f, rule.MinTerrainHeight);
        Assert.Equal(-5f, rule.MaxTemperature);
        Assert.Equal("MountainsSnowCaps", rule.ResultBiome);
        Assert.Contains("Mountains", rule.BaseBiomes);
    }

    [Fact]
    public void RefinementRule_Defaults_AreDisabledValues()
    {
        var rule = new RefinementRule();

        Assert.Equal(-1, rule.MinForestMapValue);
        Assert.Equal(-1, rule.MaxForestMapValue);
        Assert.Equal(-1, rule.MinOceanMapValue);
        Assert.Equal(-1, rule.MaxOceanMapValue);
        Assert.Equal(-1, rule.MinShrubMapValue);
        Assert.Equal(-1, rule.MaxShrubMapValue);
        Assert.True(float.IsNaN(rule.MinTemperature));
        Assert.True(float.IsNaN(rule.MaxTemperature));
        Assert.True(float.IsNaN(rule.MinRainfall));
        Assert.True(float.IsNaN(rule.MaxRainfall));
        Assert.Equal(-1, rule.MinTerrainHeight);
        Assert.Equal(-1, rule.MaxTerrainHeight);
        Assert.Equal(0, rule.Priority);
        Assert.Empty(rule.BaseBiomes);
    }

    [Fact]
    public void Deserialize_EmptyConfig_HasEmptyCollections()
    {
        var json = """{ "LandformToBiome": {}, "Refinements": {} }""";
        var config = JsonConvert.DeserializeObject<LandformBiomeConfig>(json)!;

        Assert.Empty(config.LandformToBiome);
        Assert.Empty(config.Refinements);
    }
}
