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
}
