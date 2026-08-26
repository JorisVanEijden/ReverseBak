namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The once-ever seed pass for a ref pair.
/// </summary>
public class EncounterObjectStatesSeedTests {
    private const int RefPair = 2;
    private static readonly long[] Records = { 40, 41 };

    private static IReadOnlyList<short> Roster(params short[] slots) {
        var full = new short[EncounterObjectStates.SlotsPerRecord];
        for (var i = 0; i < full.Length; i++) {
            full[i] = i < slots.Length ? slots[i] : (short)-1;
        }
        return full;
    }

    private static bool Seed(EncounterObjectStates states, params int[] dead) {
        var deadSet = new HashSet<int>(dead);
        return states.Seed(RefPair, Records,
            id => id == 40 ? Roster(100, 101, 102) : Roster(200, -1, 202),
            actor => deadSet.Contains(actor));
    }

    private static int KindAt(EncounterObjectStates states, int record, int slot) =>
        states[EncounterObjectStates.IndexOf(RefPair, record, slot)].Kind;

    [Fact]
    public void LivingRosterEntriesBecomePendingAndTheRestAreLeftAlone() {
        var states = new EncounterObjectStates();
        Assert.True(Seed(states));

        int pending = EncounterActorSpawn.KindOf(EncounterActorSpawn.Pending);
        Assert.Equal(pending, KindAt(states, 0, 1));
        Assert.Equal(pending, KindAt(states, 0, 2));
        Assert.Equal(pending, KindAt(states, 1, 0));
        Assert.Equal(pending, KindAt(states, 1, 2));

        Assert.Equal(0, KindAt(states, 1, 1));   // roster slot -1
        Assert.Equal(0, KindAt(states, 0, 3));   // beyond the named entries
    }

    [Fact]
    public void ItSeedsFromTheLIVINGSoAWipedGroupNeverComesBack() {
        // *** The combatant table remembers, not the encounter record. *** Seeding from the record
        // alone would repopulate a group the party has already killed.
        var states = new EncounterObjectStates();
        Seed(states, dead: new[] { 100, 101, 102 });

        for (var slot = 0; slot < 3; slot++) {
            Assert.NotEqual(EncounterActorSpawn.KindOf(EncounterActorSpawn.Pending),
                KindAt(states, 0, slot));
        }
        Assert.Equal(EncounterActorSpawn.KindOf(EncounterActorSpawn.Pending), KindAt(states, 1, 0));
    }

    [Fact]
    public void ThePassRunsONCEEver() {
        var states = new EncounterObjectStates();
        Assert.True(Seed(states));
        Assert.False(Seed(states), "a second visit falls straight through");
    }

    [Fact]
    public void ALIVEFirstSlotEndsUpPendingRatherThanKeepingTheMarker() {
        // *** ORDERING. *** The marker is stamped into slot 0 BEFORE the walk, and the walk
        // overwrites it. Writing it afterwards would clobber a live first actor every time — and the
        // only invariant that matters is that slot 0 is not kind 0 afterwards.
        var states = new EncounterObjectStates();
        Seed(states);

        Assert.Equal(EncounterActorSpawn.KindOf(EncounterActorSpawn.Pending), KindAt(states, 0, 0));
        Assert.False(EncounterActorSpawn.NeedsSeeding(
            states[EncounterObjectStates.IndexOf(RefPair, 0, 0)].KindState));
    }

    [Fact]
    public void ADEADFirstSlotLeavesTheMarkerAndStillCountsAsSeeded() {
        // The other half: the marker is what guarantees the invariant when the walk writes nothing.
        var states = new EncounterObjectStates();
        Seed(states, dead: new[] { 100 });

        Assert.Equal(EncounterObjectStates.KindRemoved, KindAt(states, 0, 0));
        Assert.False(EncounterActorSpawn.NeedsSeeding(
            states[EncounterObjectStates.IndexOf(RefPair, 0, 0)].KindState));
        Assert.False(Seed(states));
    }

    [Fact]
    public void ItStopsAtFiveRecordsRatherThanWalkingIntoTheNextRefPair() {
        // Deliberate divergence: the original's seed walk has no cap while its placement loop stops
        // at five, so a sixth encounter would write past the ref pair's block. Undefined behaviour
        // is not a rule to reproduce.
        var states = new EncounterObjectStates();
        var six = new long[] { 1, 2, 3, 4, 5, 6 };
        states.Seed(RefPair, six, _ => Roster(1, 2, 3, 4, 5, 6, 7), _ => false);

        // Nothing written into the next ref pair's first entries.
        Assert.Equal(0, states[EncounterObjectStates.IndexOf(RefPair + 1, 0, 0)].Kind);
        Assert.Equal(EncounterActorSpawn.KindOf(EncounterActorSpawn.Pending),
            KindAt(states, EncounterObjectStates.RecordsPerRefPair - 1, 6));
    }

    [Fact]
    public void SeedingOneRefPairLeavesTheOthersUntouched() {
        var states = new EncounterObjectStates();
        Seed(states);

        Assert.Equal(0, states[EncounterObjectStates.IndexOf(RefPair + 1, 0, 1)].Kind);
        Assert.Equal(0, states[EncounterObjectStates.IndexOf(0, 0, 1)].Kind);
    }

    [Fact]
    public void ThePendingKindIsTheSameNumberTheResetWrites() {
        // Asserted rather than assumed: two names for 0x200, and the seed uses the spawn's name
        // because this is not a reset.
        Assert.Equal(EncounterObjectStates.KindReset,
            EncounterActorSpawn.KindOf(EncounterActorSpawn.Pending));
    }
}
