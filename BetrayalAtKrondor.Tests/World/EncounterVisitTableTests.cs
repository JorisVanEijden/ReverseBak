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

    [Fact]
    public void ItSurvivesARoundTripThroughASaveBody() {
        var body = new byte[EncounterVisitTable.BodyOffset + EncounterVisitTable.SaveSize];
        var written = new EncounterVisitTable();
        written.MarkSeen(11, 13, 9, 0);
        written.MarkSeen(11, 13, 9, EncounterVisitTable.MaxEntityIndex);
        written.MarkSeen(12, 1, 2, 137);
        Assert.True(written.Save(body));

        var read = new EncounterVisitTable();
        read.Load(body);
        Assert.Equal(2, read.UsedSlots);
        Assert.True(read.HasSeen(11, 13, 9, 0));
        Assert.True(read.HasSeen(11, 13, 9, EncounterVisitTable.MaxEntityIndex));
        Assert.True(read.HasSeen(12, 1, 2, 137));
        Assert.False(read.HasSeen(11, 13, 9, 1));
        Assert.False(read.HasSeen(11, 9, 13, 0)); // x/y are not interchangeable
    }

    [Fact]
    public void TheBlockIsExactlyTheSizeItsLayoutAccountsFor() {
        // Three coordinate arrays plus the flag bitmaps. If this drifts, Load/Save would silently
        // read a neighbouring field of the save.
        Assert.Equal(EncounterVisitTable.SaveSize,
            (EncounterVisitTable.Capacity * 3)
            + (EncounterVisitTable.Capacity * EncounterVisitTable.FlagBytesPerSlot));
        // 38 bytes of flags is 304 bits, which covers the 300-record cap a WLD tile can hold.
        Assert.True(EncounterVisitTable.MaxEntityIndex >= 299);
    }

    [Fact]
    public void ATooShortBodyResetsRatherThanReadingRubbish() {
        var table = new EncounterVisitTable();
        table.MarkSeen(11, 13, 9, 0);
        table.Load(new byte[4]);
        Assert.Equal(0, table.UsedSlots);
        Assert.False(table.Save(new byte[4]));
    }

    [Fact]
    public void TheBlockSitsWhereWeSayItDoesInTheSHIPPEDFiles() {
        // *** The test that matters, and the one a round-trip cannot replace. *** Load/Save are
        // symmetric, so they agree with each other even when the offset is wrong — which it was:
        // 0xb3b is where the block lands in a SAVE##.GAM FILE (past the 100-byte header), not in
        // the body, and using it as a body offset read 100 bytes into the wrong field.
        //
        // A free table is 120 bytes of 0xff (three coordinate arrays) followed by cleared flags, so
        // the shipped files can be asked directly. TEMP.GAM is the bare body; STARTUP.GAM is the
        // same body behind a save header.
        byte[]? temp = ReadGameFile("TEMP.GAM");
        byte[]? startup = ReadGameFile("STARTUP.GAM");
        if (temp == null || startup == null) {
            return; // skip-if-absent, like the other game-data tests
        }

        Assert.Equal(startup.Length - temp.Length, EncounterVisitTable.FileOffset - EncounterVisitTable.BodyOffset);
        AssertFreeTableAt(temp, EncounterVisitTable.BodyOffset, "TEMP.GAM (body)");
        AssertFreeTableAt(startup, EncounterVisitTable.FileOffset, "STARTUP.GAM (file)");

        // And a new game really does start with every slot free.
        var table = new EncounterVisitTable();
        table.Load(temp);
        Assert.Equal(0, table.UsedSlots);
    }

    private static void AssertFreeTableAt(byte[] data, int offset, string what) {
        for (var i = 0; i < EncounterVisitTable.Capacity * 3; i++) {
            Assert.True(data[offset + i] == EncounterVisitTable.FreeMarker,
                $"{what}: expected a free-slot marker at +{i} of the block at 0x{offset:x}");
        }
        // The byte before must NOT be a marker, or the block could start anywhere in a longer run.
        Assert.NotEqual(EncounterVisitTable.FreeMarker, data[offset - 1]);
        Assert.NotEqual(EncounterVisitTable.FreeMarker, data[offset + (EncounterVisitTable.Capacity * 3)]);
    }

    private static byte[]? ReadGameFile(string name) {
        var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null) {
            string candidate = System.IO.Path.Combine(dir.FullName, "OriginalGame", name);
            if (System.IO.File.Exists(candidate)) {
                return System.IO.File.ReadAllBytes(candidate);
            }
            dir = dir.Parent;
        }
        return null;
    }
}
