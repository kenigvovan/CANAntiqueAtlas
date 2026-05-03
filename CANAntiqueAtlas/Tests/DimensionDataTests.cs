using CANAntiqueAtlas.src.core;

namespace CANAntiqueAtlas.Tests;

/// <summary>
/// Tests for DimensionData tile operations (SetTile, GetTile, RemoveTile).
/// DimensionData delegates to TileGroup, automatically creating groups as needed.
/// </summary>
public class DimensionDataTests
{
    [Fact]
    public void SetTile_GetTile_RoundTrips()
    {
        var data = new DimensionData();

        data.SetTile(10, 20, new Tile(5));

        var tile = data.GetTile(10, 20);
        Assert.NotNull(tile);
        Assert.Equal(5, tile.biomeID);
    }

    [Fact]
    public void GetTile_Empty_ReturnsNull()
    {
        var data = new DimensionData();

        Assert.Null(data.GetTile(10, 20));
    }

    [Fact]
    public void RemoveTile_ReturnsRemovedTile()
    {
        var data = new DimensionData();
        data.SetTile(10, 20, new Tile(5));

        var removed = data.RemoveTile(10, 20);

        Assert.NotNull(removed);
        Assert.Equal(5, removed.biomeID);
    }

    [Fact]
    public void RemoveTile_TileNoLongerExists()
    {
        var data = new DimensionData();
        data.SetTile(10, 20, new Tile(5));

        data.RemoveTile(10, 20);

        Assert.Null(data.GetTile(10, 20));
    }

    [Fact]
    public void RemoveTile_NonExistent_ReturnsNull()
    {
        var data = new DimensionData();

        Assert.Null(data.RemoveTile(10, 20));
    }

    [Fact]
    public void HasTileAt_ReturnsTrueWhenExists()
    {
        var data = new DimensionData();
        data.SetTile(10, 20, new Tile(5));

        Assert.True(data.HasTileAt(10, 20));
    }

    [Fact]
    public void HasTileAt_ReturnsFalseWhenEmpty()
    {
        var data = new DimensionData();

        Assert.False(data.HasTileAt(10, 20));
    }

    [Fact]
    public void SetTile_NegativeCoords_Works()
    {
        var data = new DimensionData();

        data.SetTile(-5, -10, new Tile(3));

        var tile = data.GetTile(-5, -10);
        Assert.NotNull(tile);
        Assert.Equal(3, tile.biomeID);
    }

    [Fact]
    public void SetTile_SpanningMultipleGroups_AllRetrievable()
    {
        var data = new DimensionData();

        // These should land in different TileGroups (CHUNK_STEP=16)
        data.SetTile(0, 0, new Tile(1));
        data.SetTile(20, 20, new Tile(2));
        data.SetTile(40, 40, new Tile(3));

        Assert.Equal(1, data.GetTile(0, 0)!.biomeID);
        Assert.Equal(2, data.GetTile(20, 20)!.biomeID);
        Assert.Equal(3, data.GetTile(40, 40)!.biomeID);
    }

    [Fact]
    public void RemoveTile_DoesNotAffectOtherTiles()
    {
        var data = new DimensionData();
        data.SetTile(1, 1, new Tile(10));
        data.SetTile(2, 2, new Tile(20));

        data.RemoveTile(1, 1);

        Assert.Null(data.GetTile(1, 1));
        Assert.Equal(20, data.GetTile(2, 2)!.biomeID);
    }

    [Fact]
    public void SetTile_OverwritesExisting()
    {
        var data = new DimensionData();
        data.SetTile(5, 5, new Tile(1));
        data.SetTile(5, 5, new Tile(9));

        Assert.Equal(9, data.GetTile(5, 5)!.biomeID);
    }
}
