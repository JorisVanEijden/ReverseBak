namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>Clicking a fallen encounter actor to loot it.</summary>
public class EncounterCorpseLootTests {
    [Fact]
    public void TheHotspotSlotCarriesBOTHCoordinates() {
        // *** Record above, roster slot below. *** Reading the number as a single index finds the
        // wrong body — and for any slot under seven, always the first record's.
        Assert.Equal(0, EncounterCorpseLoot.RecordIndexOf(3));
        Assert.Equal(3, EncounterCorpseLoot.RosterSlotOf(3));

        Assert.Equal(2, EncounterCorpseLoot.RecordIndexOf(16));
        Assert.Equal(2, EncounterCorpseLoot.RosterSlotOf(16));
    }

    [Fact]
    public void UndergroundReachIsWellUnderHalfTheAboveGroundOne() {
        // The same "a dungeon is a tighter space" rule the quartered underground step follows. One
        // reach everywhere lets the party loot through walls exactly where the original will not.
        Assert.Equal(7000, EncounterCorpseLoot.Reach(underground: false));
        Assert.Equal(2500, EncounterCorpseLoot.Reach(underground: true));
        Assert.True(EncounterCorpseLoot.ReachUnderground * 2 < EncounterCorpseLoot.ReachAboveGround);
    }

    [Fact]
    public void ABodyJustInsideReachIsLootableAndOneJustOutsideIsNot() {
        Assert.True(EncounterCorpseLoot.WithinReach(7000, underground: false));
        Assert.False(EncounterCorpseLoot.WithinReach(7001, underground: false));
        // The same distance flips answer underground, which is the whole point of the pair.
        Assert.False(EncounterCorpseLoot.WithinReach(3000, underground: true));
        Assert.True(EncounterCorpseLoot.WithinReach(3000, underground: false));
    }

    [Fact]
    public void AnInteractMessageOfZeroFallsBackRatherThanPlayingRecordZero() {
        // *** Zero means "no message", not "message zero". *** The original tests the subrecord
        // exists AND that its id is non-zero, so a body carrying 0 takes the default line.
        Assert.Equal(EncounterCorpseLoot.DefaultLootDialog, EncounterCorpseLoot.LootDialogFor(0));
        Assert.Equal(0x123, EncounterCorpseLoot.LootDialogFor(0x123));
    }

    [Fact]
    public void TheThreeOutcomesUseThreeDIFFERENTLines() {
        // Refused by menu state, nothing to loot, and looted are distinct messages; collapsing any
        // two of them would tell the player the wrong thing about why the click did nothing.
        Assert.NotEqual(EncounterCorpseLoot.RefusedDialog, EncounterCorpseLoot.NothingToLootDialog);
        Assert.NotEqual(EncounterCorpseLoot.NothingToLootDialog, EncounterCorpseLoot.DefaultLootDialog);
        Assert.NotEqual(EncounterCorpseLoot.RefusedDialog, EncounterCorpseLoot.DefaultLootDialog);
    }
}
