namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>What each combat HUD button does.</summary>
public class CombatCommandsTests {
    [Fact]
    public void EveryShippedCombatIdMapsToACommand() {
        // COMBAT.json ships exactly these nine.
        Assert.Equal(CombatCommands.Command.Rest, CombatCommands.For(19));   // 19 RESTS; Defend is 32
        Assert.Equal(CombatCommands.Command.Shoot, CombatCommands.For(31));
        Assert.Equal(CombatCommands.Command.Cast, CombatCommands.For(46));
        Assert.Equal(CombatCommands.Command.AutoResolve, CombatCommands.For(30));
        Assert.Equal(CombatCommands.Command.BackOrRetreat, CombatCommands.For(33));
        Assert.Equal(CombatCommands.Command.CapabilityLabel, CombatCommands.For(14));
        Assert.Equal(CombatCommands.Command.CharacterScreen, CombatCommands.For(22));
        // Identified 2026-08-22 from the describe records: 32 defends (sets Parry, clears Ready),
        // 47 inspects an enemy. Neither is an unknown mode any more.
        Assert.Equal(CombatCommands.Command.Defend, CombatCommands.For(32));
        Assert.Equal(CombatCommands.Command.Inspect, CombatCommands.For(47));
    }

    [Fact]
    public void TheseIdsCollideWithTheTravelHUDsAndMeanSomethingElse() {
        // 19 is FollowRoad on REQ_MAIN and Defend here; 46 is CastSpell there and Cast here — the
        // one coincidence, which makes the collision easy to miss. Feeding COMBAT.DAT through the
        // travel screen's switch would fire the travel action.
        Assert.Equal(CombatCommands.Command.Rest, CombatCommands.For(19));   // 19 RESTS; Defend is 32
        Assert.NotEqual(CombatCommands.Command.None, CombatCommands.For(46));
    }

    [Fact]
    public void AnIdFromAnotherScreenIsNotACombatCommand() {
        Assert.Equal(CombatCommands.Command.None, CombatCommands.For(18));
        Assert.Equal(CombatCommands.Command.None, CombatCommands.For(48));
    }

    [Fact]
    public void AutoResolveIsRefusedWhileATrapPuzzleObjectiveRemains() {
        // You cannot hand a puzzle to the AI and have it walk out for you. The button does nothing,
        // with no message at all — so a port that shows a refusal is adding one.
        Assert.True(CombatCommands.AutoResolveAllowed(gridHasObjective: false));
        Assert.False(CombatCommands.AutoResolveAllowed(gridHasObjective: true));
    }

    [Fact]
    public void TheBackButtonDependsOnWhichMenuIsShowing() {
        Assert.True(CombatCommands.BacksOutOfShootMenu(shootMenuIsUp: true));
        Assert.False(CombatCommands.BacksOutOfShootMenu(shootMenuIsUp: false));
    }

    [Fact]
    public void TheRetreatROLLDecidesWhetherYouGetOut_ItIsNotAToll() {
        // *** Corrected mid-session. *** canassa names the roll "maybe_random_trap" and the success
        // path plays a dialog that reads like a penalty, which invites "fleeing costs you a trap".
        // The control flow says the roll IS the escape test: pass and combat cancels, fail and you
        // stay in the fight having lost your turn.
        Assert.True(CombatCommands.RetreatSucceeded(escapeRollPassed: true));
        Assert.False(CombatCommands.RetreatSucceeded(escapeRollPassed: false));
        Assert.True(CombatCommands.FailedRetreatSpendsTheTurn);
    }

    [Fact]
    public void ADeadPartyMemberBlocksTheEscapeOutright() {
        // The loop breaks on the first dead party member and never reaches the roll. A party that
        // has already lost someone is committed to the fight — the opposite of the intuition that
        // losing makes you likelier to run, so it is asserted with a roll that would otherwise pass.
        Assert.False(CombatCommands.EscapeRollPasses(
            anyPartyMemberDead: true, roll: 0, trapsLoaded: true));
    }

    [Fact]
    public void TheRollIsFiftyPercentAndExclusiveAtTheBoundary() {
        Assert.True(CombatCommands.EscapeRollPasses(false, roll: 49, trapsLoaded: true));
        Assert.False(CombatCommands.EscapeRollPasses(false, roll: 50, trapsLoaded: true));
        Assert.True(CombatCommands.EscapeRollPasses(false, roll: 0, trapsLoaded: true));
    }

    [Fact]
    public void WithoutTheTrapDataTheEscapeFailsHoweverTheRollWent() {
        // Tested LAST and ANDed with the rest, so it overrides a passing roll.
        Assert.False(CombatCommands.EscapeRollPasses(false, roll: 0, trapsLoaded: false));
        Assert.False(CombatCommands.EscapeRollPasses(false, roll: 99, trapsLoaded: false));
    }


    [Fact]
    public void TheThreeRetreatDialogsAreDistinct() {
        Assert.NotEqual(CombatCommands.RetreatEscapeDialog, CombatCommands.RetreatRefusedDialog);
        Assert.NotEqual(CombatCommands.RetreatEscapeDialog, CombatCommands.RetreatRefusedMismatchDialog);
        Assert.NotEqual(CombatCommands.RetreatRefusedDialog, CombatCommands.RetreatRefusedMismatchDialog);
    }

    [Fact]
    public void ShootAndCastAgreeWithTheCapabilitySlotTheyAppearIn() {
        // The two live faces of COMBAT's shared cell are these same commands, so the slot model and
        // the command model cannot drift apart.
        Assert.Equal(CombatCommands.ShootId, CombatMenuSlots.ShootActionId);
        Assert.Equal(CombatCommands.CastId, CombatMenuSlots.CastActionId);
        Assert.Equal(CombatCommands.CapabilityLabelId, CombatMenuSlots.NeitherActionId);
    }
}
