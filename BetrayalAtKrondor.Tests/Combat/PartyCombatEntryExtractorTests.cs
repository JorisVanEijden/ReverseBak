namespace BetrayalAtKrondor.Tests.Combat;

using System.IO;
using GameData.Resources.Combat;
using GameData.Resources.Data;
using ResourceExtraction.Extractors;
using Xunit;

/// <summary>
/// Parsing P1.DAT — the party's combat entry states. The reference bytes are the real shipped file,
/// verbatim, so a layout change fails here rather than silently moving everyone on the grid.
/// </summary>
public class PartyCombatEntryExtractorTests {
    // OriginalGame/P1.DAT (132 bytes), verbatim.
    private static readonly byte[] ShippedBytes = {
        0x00, 0x00, 0x11, 0x00, 0x01, 0x01, 0xff, 0xff, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0xff, 0x01, 0x05, 0x01, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x0f, 0x00, 0x06, 0x02, 0xff, 0xff, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0xff, 0x01, 0x02, 0x01, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x10, 0x00, 0x04, 0x00, 0xff, 0xff, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0xff, 0x08, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x2d, 0x00, 0x04, 0x00, 0xff, 0xff, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0xff, 0x08, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x33, 0x00, 0x01, 0x01, 0xff, 0xff, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0xff, 0x01, 0x05, 0x01, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x2f, 0x00, 0x06, 0x02, 0xff, 0xff, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0xff, 0x01, 0x02, 0x01, 0x00, 0x00, 0x00, 0x00
    };

    private static PartyCombatEntries Parse() =>
        new PartyCombatEntryExtractor().Extract("P1.DAT", new MemoryStream(ShippedBytes));

    [Fact]
    public void TheShippedFileIsSixCombatantStateRecords() {
        Assert.Equal(132, ShippedBytes.Length);
        Assert.Equal(PartyCombatEntries.SlotCount, 132 / CombatRecordReader.RecordSize);
        Assert.Equal(PartyCombatEntries.SlotCount, Parse().Slots.Count);
    }

    [Fact]
    public void EachSlotCarriesItsEntryTileAndCreatureType() {
        var entries = Parse();
        Assert.Equal(17, entries.Slots[0].CreatureType);
        Assert.Equal(1, entries.Slots[0].XOnGrid);
        Assert.Equal(1, entries.Slots[0].YOnGrid);
        Assert.Equal(15, entries.Slots[1].CreatureType);
        Assert.Equal(6, entries.Slots[1].XOnGrid);
        Assert.Equal(2, entries.Slots[1].YOnGrid);
    }

    [Fact]
    public void EntryTilesREPEATAcrossSlots_WhichIsWhyPlacementIsAPass() {
        // *** Not a parsing error. *** (1,1), (6,2) and (4,0) each appear twice, so the party
        // collides with ITSELF on entry and combat_actor_place_on_free_tile has to resolve it.
        // A port that trusts these as final positions stacks party members on top of each other.
        var e = Parse().Slots;
        Assert.Equal((e[0].XOnGrid, e[0].YOnGrid), (e[4].XOnGrid, e[4].YOnGrid));
        Assert.Equal((e[1].XOnGrid, e[1].YOnGrid), (e[5].XOnGrid, e[5].YOnGrid));
        Assert.Equal((e[2].XOnGrid, e[2].YOnGrid), (e[3].XOnGrid, e[3].YOnGrid));
    }

    [Fact]
    public void NobodyStartsWithATarget() {
        // 0xff/0xff is the "no target" marker, so an entering party member is not mid-fight.
        foreach (SaveGameCombatData slot in Parse().Slots) {
            Assert.Equal(0xff, slot.TargetXOnGrid);
            Assert.Equal(0xff, slot.TargetYOnGrid);
        }
    }

    [Fact]
    public void CharSlotIsOneBased() {
        // The original seeks to (charSlot - 1) * sizeof(CombatantState); slot 0 is "not in the party".
        var entries = Parse();
        Assert.Same(entries.Slots[0], entries.EntryFor(1));
        Assert.Same(entries.Slots[5], entries.EntryFor(6));
        Assert.Null(entries.EntryFor(0));
        Assert.Null(entries.EntryFor(7));
    }

    [Fact]
    public void AShortFileYieldsFewerSlotsRatherThanThrowing() {
        var truncated = new byte[CombatRecordReader.RecordSize * 2 + 5];
        Assert.Equal(2, new PartyCombatEntryExtractor()
            .Extract("P1.DAT", new MemoryStream(truncated)).Slots.Count);
    }
}
