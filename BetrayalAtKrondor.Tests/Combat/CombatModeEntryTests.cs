namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// What starting a fight does: unload the world, pick one of three songs, and decide which actor
/// table is whose.
/// </summary>
public class CombatModeEntryTests {
    [Fact]
    public void TheWorldIsUnloadedNotHidden() {
        // So leaving a fight is a load rather than a resume.
        Assert.True(CombatModeEntry.UnloadsTheWorldZone);
    }

    [Fact]
    public void CombatMusicIsOneOfThreeTracks() {
        Assert.Equal(3, CombatModeEntry.CombatSongs.Length);
        Assert.Equal(1034, CombatModeEntry.SongFor(0));
        Assert.Equal(1005, CombatModeEntry.SongFor(1));
        Assert.Equal(1043, CombatModeEntry.SongFor(2));
    }

    [Fact]
    public void AndTheyAreAllDistinctSoAReplaySoundsDifferent() {
        var seen = new System.Collections.Generic.HashSet<int>(CombatModeEntry.CombatSongs);
        Assert.Equal(3, seen.Count);
    }

    [Fact]
    public void TurningTheOptionOffGoesSilentRatherThanLeavingTheTrack() {
        Assert.Equal(CombatModeEntry.NoSong,
            CombatModeEntry.SongToPlay(rollUnder3: 1, combatMusicEnabled: false));
        Assert.Equal(1005, CombatModeEntry.SongToPlay(rollUnder3: 1, combatMusicEnabled: true));
    }

    [Fact]
    public void TableAIsTheMonstersAndTableBIsTheParty() {
        // Which is what makes a monster's heal scan its own side and the target picker scan the
        // party.
        // Corrected 2026-08-20: this asserted the opposite. Table A is copied from the save's
        // active party; table B is read from the encounter roster (creature types, monster names).
        // The earlier reading mistook the AI's heal scan — which goes through the ACTING-SIDE
        // POINTER, not the array — for evidence about which array is which.
        Assert.True(CombatModeEntry.TableAIsThePartyRoster);
    }

    [Fact]
    public void EachSideHasSevenSlots() {
        Assert.Equal(7, CombatModeEntry.SideSlots);
    }

    [Fact]
    public void IdleAnimationsDoNotBreatheInUnison() {
        Assert.True(CombatModeEntry.IdleAnimationsAreRandomlyPhased);
    }

    [Fact]
    public void TheSpellCatalogueOutlivesACastInCombatButNotInTheField() {
        // Two different lifetimes for the same data, decided by where you are casting from.
        Assert.True(CombatModeEntry.CatalogueIsResidentForTheEncounter);
        Assert.True(FieldSpells.CatalogueIsLoadedOnlyForTheCastScreen);
    }
    [Fact]
    public void TheOverworldTrackResumesRatherThanRestarting() {
        // Entry keeps whatever the music call returned; exit passes it back.
        Assert.True(CombatModeEntry.PreviousSongIsRestoredOnExit);
    }

    [Fact]
    public void NothingASpellHungOnACombatantSurvivesTheFight() {
        Assert.True(CombatModeEntry.EffectPoolIsDisposedOnExit);
    }

    [Fact]
    public void TheWorldIsReloadedOnTheWayOut() {
        Assert.True(CombatModeEntry.UnloadsTheWorldZone);
        Assert.True(CombatModeEntry.ReloadsTheWorldZoneOnExit);
    }

    [Fact]
    public void TeardownReleasesEverythingEntryAcquired() {
        // The pairing is the point: acquiring these at different lifetimes than the original is how
        // something ends up outliving an encounter.
        Assert.Equal(12, CombatModeEntry.TeardownOrder.Length);
        Assert.Equal("spell weakness/resistance tables", CombatModeEntry.TeardownOrder[0]);
        Assert.Equal("world zone (reloaded)",
            CombatModeEntry.TeardownOrder[CombatModeEntry.TeardownOrder.Length - 1]);
    }

    [Fact]
    public void APartyMemberCannotDieInCombat() {
        // The teardown walks table A — the party — and rewrites anyone below 1 health to 1 health,
        // 0 stamina. A port that lets the encounter kill a member has invented a rule.
        Assert.True(CombatModeEntry.PartyMembersLeaveCombatAlive);
        Assert.Equal(1, CombatModeEntry.DownedPartyMemberHealth);
        Assert.Equal(0, CombatModeEntry.DownedPartyMemberStamina);
    }
}
