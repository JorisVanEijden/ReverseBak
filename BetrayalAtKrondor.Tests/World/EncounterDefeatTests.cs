namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;

using System.Collections.Generic;
using System.Linq;

using Xunit;

/// <summary>
/// Marking an encounter defeated — <c>rgnenc_mark_defended</c>.
/// </summary>
public class EncounterDefeatTests {
    [Fact]
    public void ZeroMeansEveryEncounter_NotRecordZero() {
        // The original skips a record when (filter != 0 && filter != id), so 0 disables the filter.
        // Reading it as an ordinary id defeats exactly one encounter where the game defeats them all.
        Assert.True(EncounterDefeat.Matches(EncounterDefeat.AllEncounters, 17));
        Assert.True(EncounterDefeat.Matches(EncounterDefeat.AllEncounters, 0));
        Assert.True(EncounterDefeat.Matches(17, 17));
        Assert.False(EncounterDefeat.Matches(17, 18));
    }

    [Fact]
    public void DefeatTakesTheRoamersOffPatrol() {
        var states = new EncounterObjectStates();
        states.SetKindForTest(2, 1, 0, EncounterObjectStates.KindRoaming);
        states.SetKindForTest(2, 1, 1, EncounterObjectStates.KindStanding);
        states.SetKindForTest(2, 1, 2, EncounterObjectStates.KindRoaming);

        EncounterDefeat.Result r = EncounterDefeat.ApplyToRecord(
            states, refPair: 2, recordIndex: 1, roster: null, isAlive: _ => false, kill: _ => { });

        Assert.Equal(2, r.ActorsStopped);
        for (var slot = 0; slot < 3; slot++) {
            Assert.Equal(EncounterObjectStates.KindStanding,
                states[EncounterObjectStates.IndexOf(2, 1, slot)].Kind);
        }
    }

    [Fact]
    public void OnlyRoamersAreTouched_NotRemovedOnes() {
        // A slot already recorded as removed must stay removed — resurrecting it as "standing"
        // would put a killed actor back on the field.
        var states = new EncounterObjectStates();
        states.MarkRemoved(0, 0, 0);
        states.SetKindForTest(0, 0, 1, EncounterObjectStates.KindRoaming);

        EncounterDefeat.ApplyToRecord(states, 0, 0, null, _ => false, _ => { });

        Assert.Equal(EncounterObjectStates.KindRemoved, states[EncounterObjectStates.IndexOf(0, 0, 0)].Kind);
        Assert.Equal(EncounterObjectStates.KindStanding, states[EncounterObjectStates.IndexOf(0, 0, 1)].Kind);
    }

    [Fact]
    public void EveryLivingRosterActorDies_EvenOneNeverOnTheField() {
        // The roster is what a later visit re-seeds from, so a survivor there would repopulate the
        // group however thoroughly the party won.
        var alive = new HashSet<int> { 10, 11, 12 };
        var states = new EncounterObjectStates();

        EncounterDefeat.Result r = EncounterDefeat.ApplyToRecord(
            states, 0, 0,
            roster: new[] { 10, EncounterDefeat.EmptyRosterSlot, 11, 12, -1, -1, -1 },
            isAlive: alive.Contains,
            kill: id => alive.Remove(id));

        Assert.Equal(new[] { 10, 11, 12 }, r.ActorsKilled.ToArray());
        Assert.Empty(alive);
    }

    [Fact]
    public void AnAlreadyDeadActorIsNotRewritten() {
        // The original tests the flag first. Writing the record back would rewrite a combatant the
        // party may have already looted.
        var killed = new List<int>();
        var states = new EncounterObjectStates();

        EncounterDefeat.Result r = EncounterDefeat.ApplyToRecord(
            states, 0, 0,
            roster: new[] { 10, 11 },
            isAlive: id => id == 11,
            kill: killed.Add);

        Assert.Equal(new[] { 11 }, killed.ToArray());
        Assert.Equal(new[] { 11 }, r.ActorsKilled.ToArray());
    }

    [Fact]
    public void EmptySlotsAreSkipped() {
        var killed = new List<int>();
        var states = new EncounterObjectStates();

        EncounterDefeat.ApplyToRecord(states, 0, 0,
            roster: Enumerable.Repeat(EncounterDefeat.EmptyRosterSlot, 7).ToArray(),
            isAlive: _ => true, kill: killed.Add);

        Assert.Empty(killed);
    }
}
