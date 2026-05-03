using CANAntiqueAtlas.src.core;

namespace CANAntiqueAtlas.Tests;

/// <summary>
/// Tests for TileGroup: SetTile, GetTile, RemoveTile, and coordinate encoding.
/// </summary>
public class TileGroupTests
{
    [Fact]
    public void SetTile_GetTile_RoundTrips()
    {
        var group = new TileGroup(0, 0);

        group.SetTile(3, 5, new Tile(7));

        var tile = group.GetTile(3, 5);
        Assert.NotNull(tile);
        Assert.Equal(7, tile.biomeID);
    }

    [Fact]
    public void GetTile_Empty_ReturnsNull()
    {
        var group = new TileGroup(0, 0);

        Assert.Null(group.GetTile(3, 5));
    }

    [Fact]
    public void SetTile_OutsideScope_DoesNothing()
    {
        var group = new TileGroup(0, 0);

        // Scope is [0, CHUNK_STEP-1], so 20 is outside for CHUNK_STEP=16
        group.SetTile(20, 20, new Tile(7));

        Assert.Null(group.GetTile(20, 20));
    }

    [Fact]
    public void SetTile_OverwritesExisting()
    {
        var group = new TileGroup(0, 0);

        group.SetTile(2, 3, new Tile(1));
        group.SetTile(2, 3, new Tile(5));

        var tile = group.GetTile(2, 3);
        Assert.NotNull(tile);
        Assert.Equal(5, tile.biomeID);
    }

    [Fact]
    public void RemoveTile_ReturnsRemovedTile()
    {
        var group = new TileGroup(0, 0);
        group.SetTile(4, 6, new Tile(9));

        var removed = group.RemoveTile(4, 6);

        Assert.NotNull(removed);
        Assert.Equal(9, removed.biomeID);
    }

    [Fact]
    public void RemoveTile_TileNoLongerExists()
    {
        var group = new TileGroup(0, 0);
        group.SetTile(4, 6, new Tile(9));

        group.RemoveTile(4, 6);

        Assert.Null(group.GetTile(4, 6));
    }

    [Fact]
    public void RemoveTile_NonExistent_ReturnsNull()
    {
        var group = new TileGroup(0, 0);

        var removed = group.RemoveTile(4, 6);

        Assert.Null(removed);
    }

    [Fact]
    public void RemoveTile_OutsideScope_ReturnsNull()
    {
        var group = new TileGroup(0, 0);
        group.SetTile(3, 3, new Tile(1));

        var removed = group.RemoveTile(20, 20);

        Assert.Null(removed);
    }

    [Fact]
    public void SetTile_WithOffset_WorksCorrectly()
    {
        // TileGroup with offset origin
        int offsetX = 16;
        int offsetY = 32;
        var group = new TileGroup(offsetX, offsetY);

        group.SetTile(offsetX + 3, offsetY + 5, new Tile(11));

        var tile = group.GetTile(offsetX + 3, offsetY + 5);
        Assert.NotNull(tile);
        Assert.Equal(11, tile.biomeID);
    }

    [Fact]
    public void SetTile_MultipleTiles_AllRetrievable()
    {
        var group = new TileGroup(0, 0);

        group.SetTile(0, 0, new Tile(1));
        group.SetTile(5, 5, new Tile(2));
        group.SetTile(15, 15, new Tile(3));

        Assert.Equal(1, group.GetTile(0, 0)!.biomeID);
        Assert.Equal(2, group.GetTile(5, 5)!.biomeID);
        Assert.Equal(3, group.GetTile(15, 15)!.biomeID);
    }

    [Fact]
    public void RemoveTile_DoesNotAffectOtherTiles()
    {
        var group = new TileGroup(0, 0);
        group.SetTile(1, 1, new Tile(10));
        group.SetTile(2, 2, new Tile(20));

        group.RemoveTile(1, 1);

        Assert.Null(group.GetTile(1, 1));
        Assert.Equal(20, group.GetTile(2, 2)!.biomeID);
    }

    [Fact]
    public void Scope_MatchesConstructorParameters()
    {
        var group = new TileGroup(32, 48);
        var scope = group.GetScope();

        Assert.Equal(32, scope.minX);
        Assert.Equal(48, scope.minY);
        Assert.Equal(32 + TileGroup.CHUNK_STEP - 1, scope.maxX);
        Assert.Equal(48 + TileGroup.CHUNK_STEP - 1, scope.maxY);
    }
}
