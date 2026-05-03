using CANAntiqueAtlas.src.core.BiomeDetectors;

namespace CANAntiqueAtlas.Tests;

/// <summary>
/// Tests for BiomeDetectorBase registration logic and deterministic selection.
/// GetBiomeID requires full VS runtime and is not unit-testable,
/// but we can verify registration and the voting/selection logic.
/// </summary>
public class BiomeDetectorBaseTests
{
    // ========== Registration tests ==========

    [Fact]
    public void RegisterBiome_AddedToBiomeConditionsSet()
    {
        var detector = new BiomeDetectorBase();

        var plains = new BiomeConditions
        {
            Name = "plains",
            BiomeName = "Plains",
            BiomeId = 3,
            Priority = 1,
            MinTemperature = -15,
            MaxTemperature = 25,
            MinRainfall = 0,
            MaxRainfall = 1,
            MinFertility = 0,
            MaxFertility = 1
        };

        detector.BiomeConditionsSet.Add(plains);

        Assert.Contains(plains, detector.BiomeConditionsSet);
    }

    [Fact]
    public void RegisterBiome_BlockScanBiome_AddedToSet()
    {
        var detector = new BiomeDetectorBase();

        var hotspring = new BiomeConditions
        {
            Name = "HotSpring",
            BiomeName = "HotSpring",
            BiomeId = 9,
            Priority = 4,
            BlockCodeWildCards = new() { "sludgygravel" },
            MinTemperature = -35,
            MaxTemperature = 55,
            MinRainfall = 0,
            MaxRainfall = 1,
            MinFertility = 0,
            MaxFertility = 1
        };

        detector.BiomeConditionsSet.Add(hotspring);

        Assert.Contains(hotspring, detector.BiomeConditionsSet);
        Assert.True(hotspring.BlockCodeWildCards.Count > 0);
    }

    [Fact]
    public void RegisterBiome_MultipleBiomes_AllRegistered()
    {
        var detector = new BiomeDetectorBase();

        var plains = new BiomeConditions { Name = "plains", BiomeId = 3 };
        var jungle = new BiomeConditions { Name = "jungle", BiomeId = 12 };

        detector.BiomeConditionsSet.Add(plains);
        detector.BiomeConditionsSet.Add(jungle);

        Assert.Equal(2, detector.BiomeConditionsSet.Count);
    }

    // ========== FinalSelectionPriority tests ==========

    [Fact]
    public void FinalSelectionPriority_CanSetAndRetrieve()
    {
        var detector = new BiomeDetectorBase();

        detector.FinalSelectionPriority[9] = 1.0f;
        detector.FinalSelectionPriority[11] = 0.99f;

        Assert.Equal(1.0f, detector.FinalSelectionPriority[9]);
        Assert.Equal(0.99f, detector.FinalSelectionPriority[11]);
    }

    [Fact]
    public void FinalSelectionPriority_OverwriteExistingKey()
    {
        var detector = new BiomeDetectorBase();

        detector.FinalSelectionPriority[9] = 0.5f;
        detector.FinalSelectionPriority[9] = 1.0f;

        Assert.Equal(1.0f, detector.FinalSelectionPriority[9]);
    }

    // ========== Deterministic tie-breaking simulation ==========
    // These replicate the voting selection logic from BiomeDetectorBase.GetBiomeID
    // and BiomeDetectorLandform.ScanBlocksForMarkerBiomes.

    [Fact]
    public void DeterministicSelection_SameVotes_LowerBiomeIdWins()
    {
        var votes = new Dictionary<int, int>
        {
            { 12, 3 }, // Jungle
            { 11, 3 }, // Redwood — same votes
        };
        var finalPriority = new Dictionary<int, float>();

        int bestId = SelectWinner(votes, finalPriority);

        Assert.Equal(11, bestId);
    }

    [Fact]
    public void DeterministicSelection_FinalPriority_OverridesVotes()
    {
        var votes = new Dictionary<int, int>
        {
            { 3, 10 },  // Plains — 10 votes
            { 9, 2 },   // HotSpring — only 2 votes
        };
        var finalPriority = new Dictionary<int, float>
        {
            { 9, 1.0f } // HotSpring has FinalSelectionPriority
        };

        int bestId = SelectWinner(votes, finalPriority);

        Assert.Equal(9, bestId);
    }

    [Fact]
    public void DeterministicSelection_HigherVotesWin_WhenSamePriority()
    {
        var votes = new Dictionary<int, int>
        {
            { 1, 5 },   // TemperateForest — 5 votes
            { 10, 8 },  // Swamp — 8 votes
        };
        var finalPriority = new Dictionary<int, float>();

        int bestId = SelectWinner(votes, finalPriority);

        Assert.Equal(10, bestId);
    }

    [Fact]
    public void DeterministicSelection_ConsistentAcrossMultipleRuns()
    {
        var votes = new Dictionary<int, int>
        {
            { 11, 4 },
            { 12, 4 },
            { 10, 4 },
        };
        var finalPriority = new Dictionary<int, float>
        {
            { 11, 0.99f },
            { 12, 0.99f },
        };

        int first = SelectWinner(votes, finalPriority);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(first, SelectWinner(votes, finalPriority));
        }
    }

    [Fact]
    public void DeterministicSelection_SingleCandidate_AlwaysWins()
    {
        var votes = new Dictionary<int, int> { { 5, 1 } };
        var finalPriority = new Dictionary<int, float>();

        Assert.Equal(5, SelectWinner(votes, finalPriority));
    }

    [Fact]
    public void DeterministicSelection_EmptyVotes_ReturnsMinusOne()
    {
        var votes = new Dictionary<int, int>();
        var finalPriority = new Dictionary<int, float>();

        Assert.Equal(-1, SelectWinner(votes, finalPriority));
    }

    // ========== Condition matching simulation ==========

    [Fact]
    public void ConditionMatching_HigherPriorityWins()
    {
        var conditions = new List<BiomeConditions>
        {
            new() { Name = "plains", BiomeId = 3, Priority = 1.0f },
            new() { Name = "forest", BiomeId = 1, Priority = 1.3f },
        };

        // Simulate: find the highest priority condition
        BiomeConditions? best = null;
        foreach (var cond in conditions)
        {
            if (best == null || cond.Priority > best.Priority)
                best = cond;
        }

        Assert.NotNull(best);
        Assert.Equal("forest", best.Name);
    }

    // ========== Helpers ==========

    /// <summary>
    /// Replicates the deterministic selection logic from ScanBlocksForMarkerBiomes.
    /// </summary>
    private static int SelectWinner(Dictionary<int, int> biomeVotes, Dictionary<int, float> finalSelectionPriority)
    {
        int bestId = -1;
        int bestVotes = 0;
        float bestFinalPrio = -1f;

        foreach (var kvp in biomeVotes)
        {
            finalSelectionPriority.TryGetValue(kvp.Key, out float prio);

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

        return bestId;
    }
}
