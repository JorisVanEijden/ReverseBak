namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The nine-slot tile cache: what "crossing a boundary" actually does.
/// </summary>
public class WorldTileCacheTests {
    [Fact]
    public void ATilesCoordinateIsTheTruncatedDivision() {
        Assert.Equal(0, WorldTileCache.TileOf(0));
        Assert.Equal(0, WorldTileCache.TileOf(WorldTileCache.TileWorldSize - 1));
        Assert.Equal(1, WorldTileCache.TileOf(WorldTileCache.TileWorldSize));
        Assert.Equal(3, WorldTileCache.TileOf(WorldTileCache.TileWorldSize * 3 + 250));
    }

    [Fact]
    public void CrossingIsMeasuredAgainstSLOTZEROOnly() {
        // Slot 0 is the tile the party is in; the crossing test compares against it and nothing else.
        Assert.False(WorldTileCache.HasCrossed(4, 7, slotZeroX: 4, slotZeroY: 7));
        Assert.True(WorldTileCache.HasCrossed(5, 7, slotZeroX: 4, slotZeroY: 7));
        Assert.True(WorldTileCache.HasCrossed(4, 8, slotZeroX: 4, slotZeroY: 7));
    }

    [Fact]
    public void THESEARCHSKIPSSLOTZERO() {
        // *** Excluded, not merely unlikely to match. *** The scan runs 1..8, which is safe only
        // because the caller has already compared against slot 0 and returned. A port that searches
        // from 0 finds the current tile and swaps it with itself.
        Assert.False(WorldTileCache.IsSearchable(0));
        Assert.True(WorldTileCache.IsSearchable(1));
        Assert.True(WorldTileCache.IsSearchable(8));
        Assert.False(WorldTileCache.IsSearchable(9));
    }

    [Fact]
    public void CROSSINGDOESNOTLOAD() {
        // The headline, and the opposite of what the function name says. The handler returns when
        // the lookup fails: no load, no swap, no item refresh. It works only because the ring around
        // the current tile is kept populated in advance.
        Assert.False(WorldTileCache.LoadsOnCrossing);
    }

    [Fact]
    public void ANEmptySlotIsTheNormalEarlyState() {
        // All nine start zeroed and only the party's own tile is loaded at init, so an empty slot is
        // ordinary rather than a fault.
        Assert.Equal(0, WorldTileCache.EmptyZone);
        Assert.Equal(9, WorldTileCache.Slots);
    }

    [Fact]
    public void ACrossingWipesBOTHTransientHotspotBlocks() {
        // Twenty keys: the scout-TRIED flags at 5200 and the SPOTTED flags at 5210. Clearing only
        // the first leaves a spot earned on one tile buying a sneak-past on the next.
        Assert.True(WorldTileCache.ClearedOnCrossing(5200));
        Assert.True(WorldTileCache.ClearedOnCrossing(5209));
        Assert.True(WorldTileCache.ClearedOnCrossing(5210));
        Assert.True(WorldTileCache.ClearedOnCrossing(5219));

        Assert.False(WorldTileCache.ClearedOnCrossing(5199));
        Assert.False(WorldTileCache.ClearedOnCrossing(5220));
        Assert.Equal(20, WorldTileCache.LastClearedGlobal - WorldTileCache.FirstClearedGlobal + 1);
    }

    [Fact]
    public void TheItemStorageIsPartitionedPerSlot() {
        // Nine slots carved out of one allocation, which is why the cache is a fixed nine rather
        // than a dictionary that could grow.
        Assert.Equal(6600, WorldTileCache.ItemBytesPerSlot);
    }
}
