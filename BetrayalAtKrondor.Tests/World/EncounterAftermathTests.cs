namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// What happens when a combat encounter ends — <c>combTrigger_phase2</c>'s tail.
/// </summary>
public class EncounterAftermathTests {
    [Fact]
    public void OnlyTwoOfTheFourOutcomesDoAnythingAndTheyDoOppositeThings() {
        Assert.True(EncounterAftermath.RelocatesTheParty(EncounterAftermath.Outcome.PartyMoved));
        Assert.False(EncounterAftermath.RelocatesTheParty(EncounterAftermath.Outcome.Resolved));

        Assert.True(EncounterAftermath.FiresThePostEvent(EncounterAftermath.Outcome.Resolved));
        Assert.False(EncounterAftermath.FiresThePostEvent(EncounterAftermath.Outcome.PartyMoved));
    }

    [Fact]
    public void NothingAndUnhandledSettleNothingAtAll() {
        foreach (EncounterAftermath.Outcome outcome in new[] {
            EncounterAftermath.Outcome.Nothing, EncounterAftermath.Outcome.Unhandled }) {
            Assert.False(EncounterAftermath.RelocatesTheParty(outcome));
            Assert.False(EncounterAftermath.FiresThePostEvent(outcome));
            Assert.False(EncounterAftermath.ReloadsSceneAndMap(outcome));
        }
    }

    [Fact]
    public void ONLYTheTwoThatChangedSomethingRebuildTheScene() {
        // Reloading unconditionally costs a rebuild after every trivial outcome; never reloading
        // leaves a defeated encounter still standing in the world.
        Assert.True(EncounterAftermath.ReloadsSceneAndMap(EncounterAftermath.Outcome.Resolved));
        Assert.True(EncounterAftermath.ReloadsSceneAndMap(EncounterAftermath.Outcome.PartyMoved));
    }

    [Fact]
    public void TheOutcomeNumberingIsTheORIGINALS() {
        // The arena writes these, so they are data rather than an internal enum.
        Assert.Equal(0, (int)EncounterAftermath.Outcome.Nothing);
        Assert.Equal(1, (int)EncounterAftermath.Outcome.Resolved);
        Assert.Equal(2, (int)EncounterAftermath.Outcome.PartyMoved);
        Assert.Equal(3, (int)EncounterAftermath.Outcome.Unhandled);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void FIVEOfTheEightDirectionsShareTheFirstLanding(int direction) {
        // The switch has arms for 2, 4 and 8 only; everything else falls to the default, which is
        // the same entry direction 1 would have used.
        Assert.Equal(EncounterAftermath.Landing.Direction1,
            EncounterAftermath.LandingFor(direction));
    }

    [Fact]
    public void TheOtherThreeDirectionsHaveTheirOwn() {
        Assert.Equal(EncounterAftermath.Landing.Direction2, EncounterAftermath.LandingFor(2));
        Assert.Equal(EncounterAftermath.Landing.Direction4, EncounterAftermath.LandingFor(4));
        Assert.Equal(EncounterAftermath.Landing.Direction8, EncounterAftermath.LandingFor(8));
    }

    [Fact]
    public void ALandingIsAnOffsetINSIDEATileNotAWorldPosition() {
        // And the tile is the one the party is standing in when the fight ends. Treating the stored
        // value as absolute drops the party near the world origin whatever map they were on.
        Assert.Equal((12L * WorldPlacement.TileSize) + 3500,
            EncounterAftermath.WorldCoordinate(12, 3500));
        Assert.NotEqual(3500, EncounterAftermath.WorldCoordinate(12, 3500));
    }

    [Fact]
    public void TheFoughtStampLandsWhateverTheOutcome() {
        // Written before the outcome is examined. Stamping it only on a win would leave the other
        // outcomes looking like the fight never happened.
        Assert.True(EncounterAftermath.FoughtTimeIsStampedRegardless);
    }

    // ---- which outcome a finished fight reports -------------------------------------------------

    [Fact]
    public void OnlyAWinIsRESOLVED() {
        // *** This is what decides whether the encounter ever fires again. *** The fought flag had
        // been read since the activate pass was built and written by nothing, so every defeated
        // ambush stayed armed and fired on the party's next step.
        Assert.Equal(EncounterAftermath.Outcome.Resolved,
            EncounterAftermath.OutcomeFor(enemiesAlive: 0, partyAlive: 3));
        Assert.True(EncounterAftermath.FiresThePostEvent(
            EncounterAftermath.OutcomeFor(0, 3)));
    }

    [Fact]
    public void AWipeSettlesNOTHING() {
        // Ending the fight is not winning it. Marking it fought here clears an ambush the party
        // lost to — and IsOver() is true for both, which is exactly how they get conflated.
        Assert.Equal(EncounterAftermath.Outcome.Nothing,
            EncounterAftermath.OutcomeFor(enemiesAlive: 2, partyAlive: 0));
        Assert.Equal(EncounterAftermath.Outcome.Nothing,
            EncounterAftermath.OutcomeFor(enemiesAlive: 0, partyAlive: 0));
    }

    [Fact]
    public void AFightStillRunningIsNothing() {
        Assert.Equal(EncounterAftermath.Outcome.Nothing,
            EncounterAftermath.OutcomeFor(enemiesAlive: 1, partyAlive: 3));
    }

    [Fact]
    public void FLEEINGBeatsTheRoster_evenFromAFightThePartyWasWinning() {
        // Running away has not resolved the encounter however well it was going, so the flag is
        // asked first rather than inferred from who is left standing.
        Assert.Equal(EncounterAftermath.Outcome.PartyMoved,
            EncounterAftermath.OutcomeFor(enemiesAlive: 0, partyAlive: 3, partyFled: true));
        Assert.False(EncounterAftermath.FiresThePostEvent(
            EncounterAftermath.OutcomeFor(0, 3, partyFled: true)));
        Assert.True(EncounterAftermath.RelocatesTheParty(
            EncounterAftermath.OutcomeFor(0, 3, partyFled: true)));
    }
}
