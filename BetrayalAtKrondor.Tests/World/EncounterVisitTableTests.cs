namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The remembered "already came near this" bitmap behind roaming encounters. Forty tiles, no
/// eviction — the saturation behaviour is the part worth pinning.
/// </summary>
public class EncounterVisitTableTests {
    [Fact]
    public void AFreshTableRemembersNothing() {
        var table = new EncounterVisitTable();

        Assert.Equal(0, table.UsedSlots);
        Assert.False(table.HasSeen(1, 11, 11, 0));
    }

    [Fact]
    public void AMarkIsReadBackOnTheSameTileAndEntity() {
        var table = new EncounterVisitTable();

        Assert.True(table.MarkSeen(1, 11, 11, entityIndex: 7));

        Assert.True(table.HasSeen(1, 11, 11, 7));
        Assert.Equal(1, table.UsedSlots);
    }

    [Fact]
    public void EntitiesOnOneTileAreTrackedIndependently() {
        var table = new EncounterVisitTable();
        table.MarkSeen(1, 11, 11, 7);

        Assert.True(table.HasSeen(1, 11, 11, 7));
        Assert.False(table.HasSeen(1, 11, 11, 8));
        Assert.Equal(1, table.UsedSlots); // still one tile
    }

    [Fact]
    public void DifferentTilesTakeDifferentSlots() {
        var table = new EncounterVisitTable();

        table.MarkSeen(1, 11, 11, 0);
        table.MarkSeen(1, 11, 12, 0);
        table.MarkSeen(2, 11, 11, 0);

        Assert.Equal(3, table.UsedSlots);
        Assert.True(table.HasSeen(1, 11, 12, 0));
        Assert.False(table.HasSeen(1, 12, 11, 0));
    }

    [Fact]
    public void RevisitingATileReusesItsSlot() {
        var table = new EncounterVisitTable();

        for (var entity = 0; entity < 20; entity++) {
            table.MarkSeen(3, 4, 5, entity);
        }

        Assert.Equal(1, table.UsedSlots);
    }

    [Fact]
    public void AFullTableSilentlyDropsANewTile() {
        // No eviction: the forty-first tile is simply never recorded, so its encounters can fire
        // again and again. Reproduced rather than "fixed" with an LRU, which would change which
        // encounters repeat.
        var table = new EncounterVisitTable();
        for (var i = 0; i < EncounterVisitTable.Capacity; i++) {
            Assert.True(table.MarkSeen(1, (byte)i, 0, 0));
        }

        Assert.False(table.MarkSeen(1, 99, 0, 0));
        Assert.False(table.HasSeen(1, 99, 0, 0));
        Assert.Equal(EncounterVisitTable.Capacity, table.UsedSlots);
    }

    [Fact]
    public void AFullTableStillAcceptsMarksOnTilesItAlreadyKnows() {
        var table = new EncounterVisitTable();
        for (var i = 0; i < EncounterVisitTable.Capacity; i++) {
            table.MarkSeen(1, (byte)i, 0, 0);
        }

        Assert.True(table.MarkSeen(1, 0, 0, entityIndex: 5));
        Assert.True(table.HasSeen(1, 0, 0, 5));
    }

    [Fact]
    public void TheBitmapCoversAWorldTilesWholeObjectList() {
        // 38 bytes = 304 bits, against the 300-entry cap a tile's object list has.
        var table = new EncounterVisitTable();

        Assert.True(table.MarkSeen(1, 1, 1, 299));
        Assert.True(table.HasSeen(1, 1, 1, 299));
        Assert.True(EncounterVisitTable.MaxEntityIndex >= 300);
    }

    [Fact]
    public void AnOutOfRangeEntityIndexIsRefused() {
        var table = new EncounterVisitTable();

        Assert.False(table.MarkSeen(1, 1, 1, -1));
        Assert.False(table.MarkSeen(1, 1, 1, EncounterVisitTable.MaxEntityIndex + 1));
        Assert.Equal(0, table.UsedSlots);
    }

    [Fact]
    public void ResetReturnsEverySlotToFree() {
        var table = new EncounterVisitTable();
        table.MarkSeen(1, 11, 11, 7);

        table.Reset();

        Assert.Equal(0, table.UsedSlots);
        Assert.False(table.HasSeen(1, 11, 11, 7));
    }

    [Fact]
    public void TheSaveBlockIsFortyTriplesPlusItsBitmaps() {
        Assert.Equal(
            (EncounterVisitTable.Capacity * 3)
            + (EncounterVisitTable.Capacity * EncounterVisitTable.FlagBytesPerSlot),
            EncounterVisitTable.SaveSize);
    }
}
