namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>The Inspect command's follow-up click.</summary>
public class InspectActionTests {
    [Fact]
    public void InspectingAnEnemyCOSTSTheActingCharacterTheirTurn() {
        // *** The detail the help text hides. *** "Allows the current character to inspect one enemy"
        // sounds free; the code clears CAF_READY on the current actor before switching the view. A
        // port that missed this makes inspection a free action usable every round.
        InspectAction.Result r = InspectAction.Resolve(
            moveCost: 3, confirmed: true, targetIsEncounterActor: true);

        Assert.Equal(InspectAction.Result.Inspected, r);
        Assert.True(InspectAction.SpendsTheTurn(r));
        Assert.True(InspectAction.ClearsTheMode(r));
    }

    [Fact]
    public void YouCannotInspectYourOwnParty() {
        // The gate is combatenc_is_encounter_actor, so a party member under the cursor does nothing.
        InspectAction.Result r = InspectAction.Resolve(
            moveCost: 3, confirmed: true, targetIsEncounterActor: false);

        Assert.Equal(InspectAction.Result.Ignored, r);
        Assert.False(InspectAction.SpendsTheTurn(r));
    }

    [Fact]
    public void AMisclickLeavesTheModeARMED() {
        // The original resets state only on success or an explicit cancel, so clicking empty ground
        // leaves the player still choosing rather than silently wasting the command.
        InspectAction.Result r = InspectAction.Resolve(
            moveCost: 3, confirmed: true, targetIsEncounterActor: false);

        Assert.False(InspectAction.ClearsTheMode(r));
    }

    [Fact]
    public void BackingOutClearsTheModeWithoutCostingTheTurn() {
        InspectAction.Result r = InspectAction.Resolve(
            moveCost: InspectAction.CancelCost, confirmed: true, targetIsEncounterActor: true);

        Assert.Equal(InspectAction.Result.Cancelled, r);
        Assert.False(InspectAction.SpendsTheTurn(r), "backing out is free");
        Assert.True(InspectAction.ClearsTheMode(r));
    }

    [Fact]
    public void TheCancelCostIsASentinel_NotAReachableDistance() {
        // The grid is 8x13, so 1000 can only ever mean "backed out".
        Assert.Equal(1000, InspectAction.CancelCost);
        Assert.True(InspectAction.CancelCost > CombatGrid.Width * CombatGrid.Height);
    }

    [Fact]
    public void AnUnconfirmedClickDoesNothing() {
        Assert.Equal(InspectAction.Result.Ignored,
            InspectAction.Resolve(moveCost: 3, confirmed: false, targetIsEncounterActor: true));
    }
}
