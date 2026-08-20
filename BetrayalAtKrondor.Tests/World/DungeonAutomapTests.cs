namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

public class DungeonAutomapTests {
    [Fact]
    public void DirectionBitsAreAByteThenBitBitset() {
        var flags = new byte[4];
        Assert.False(DungeonAutomap.IsSeenFrom(flags, 0));
        DungeonAutomap.MarkSeenFrom(flags, 0);
        Assert.True(DungeonAutomap.IsSeenFrom(flags, 0));
        Assert.Equal(1, flags[0]);

        // direction 9 is byte 1, bit 1.
        DungeonAutomap.MarkSeenFrom(flags, 9);
        Assert.True(DungeonAutomap.IsSeenFrom(flags, 9));
        Assert.Equal(2, flags[1]);
        // ... and it did not disturb its neighbours.
        Assert.False(DungeonAutomap.IsSeenFrom(flags, 8));
        Assert.False(DungeonAutomap.IsSeenFrom(flags, 10));
    }

    [Fact]
    public void OutOfRangeDirectionsAreIgnoredRatherThanThrowing() {
        var flags = new byte[2];
        DungeonAutomap.MarkSeenFrom(flags, 999);
        DungeonAutomap.MarkSeenFrom(null, 0);
        Assert.False(DungeonAutomap.IsSeenFrom(flags, 999));
        Assert.False(DungeonAutomap.IsSeenFrom(null, 0));
        Assert.Equal(new byte[2], flags);
    }

    [Fact]
    public void AnEmptySlotIsAllFFsAndNotAllZeroes() {
        // The insert scan looks for a triple of 0xFF, not a count. Zero-filling the table makes the
        // place at 0,0,0 look occupied and loses the first sighting.
        Assert.Equal(0xFF, DungeonAutomap.EmptySlot);
        Assert.NotEqual(0, DungeonAutomap.EmptySlot);
    }

    [Fact]
    public void TheTableHoldsPlacesNotSightingsAndIsCappedAtForty() {
        // Revisiting a place from another direction adds a bit to its entry rather than a second
        // entry — which is what lets forty slots cover a dungeon.
        Assert.True(DungeonAutomap.RecordsPlacesNotSightings);
        Assert.Equal(40, DungeonAutomap.Capacity);
    }

    [Fact]
    public void OnlyUndergroundWritesTheRecordBack() {
        Assert.True(DungeonAutomap.PersistsOnlyUnderground);
    }

    [Fact]
    public void TheAutomapDrawsThroughTheMapShapeTable() {
        // Slot 2 is Z##M.TBL, added by the zone loader only underground — the simplified plan
        // geometry, not the world's own shapes.
        Assert.Equal(2, DungeonAutomap.MapShapeTableSlot);
    }

    [Fact]
    public void DoorsDrawAsMarksAndNothingElseDoes() {
        Assert.True(DungeonAutomap.DrawsAsDoorMark(0x5C));
        Assert.True(DungeonAutomap.DrawsAsDoorMark(0x5D));
        Assert.False(DungeonAutomap.DrawsAsDoorMark(0x5B));
        Assert.False(DungeonAutomap.DrawsAsDoorMark(0x5E));
    }

    [Fact]
    public void OurBuildsAutomapDoesNotDrawItsOwnPartyIcon() {
        // The centred mapicons blit is inside #ifndef V102CD and we target the CD build; the marker
        // comes from drawMap's tail instead. Copying the floppy branch draws it twice.
        Assert.False(DungeonAutomap.RendererDrawsItsOwnPartyIcon);
    }
}
