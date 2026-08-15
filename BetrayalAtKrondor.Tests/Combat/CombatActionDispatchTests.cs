namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// What a click on the combat field selects: two melee attacks on two mouse buttons, and a guard
/// action that quietly becomes a rest.
/// </summary>
public class CombatActionDispatchTests {
    [Fact]
    public void TheTwoMeleeAttacksAreOnTheTwoMouseButtons() {
        Assert.Equal(CombatActionDispatch.MeleeAttack.Thrust,
            CombatActionDispatch.AttackFor(CombatActionDispatch.LeftButton));
        Assert.Equal(CombatActionDispatch.MeleeAttack.Swing,
            CombatActionDispatch.AttackFor(CombatActionDispatch.RightButton));
        Assert.Equal(CombatActionDispatch.MeleeAttack.None, CombatActionDispatch.AttackFor(0));
    }

    [Fact]
    public void OnlyTheThrustClosesTheDistance() {
        // So the same click on the same enemy either moves you or refuses, by button.
        Assert.True(CombatActionDispatch.ApproachesTarget(CombatActionDispatch.MeleeAttack.Thrust));
        Assert.False(CombatActionDispatch.ApproachesTarget(CombatActionDispatch.MeleeAttack.Swing));
    }

    [Fact]
    public void AnExhaustedCharacterCanStillThrustButNotSwing() {
        Assert.True(CombatActionDispatch.HasReservesFor(CombatActionDispatch.MeleeAttack.Thrust,
            healthStaminaPool: 1));
        Assert.False(CombatActionDispatch.HasReservesFor(CombatActionDispatch.MeleeAttack.Swing,
            healthStaminaPool: 1));
        Assert.True(CombatActionDispatch.HasReservesFor(CombatActionDispatch.MeleeAttack.Swing,
            healthStaminaPool: 2));
    }

    [Fact]
    public void ReachAndMovementAreTheSameBudget() {
        // You cannot strike something you could not have walked to.
        Assert.True(CombatActionDispatch.WithinReach(cursorDistance: 3, movementAllowance: 3));
        Assert.False(CombatActionDispatch.WithinReach(cursorDistance: 4, movementAllowance: 3));
    }

    [Fact]
    public void WalkingIntoTroubleCostsTheThrust() {
        Assert.False(CombatActionDispatch.ThrustSurvivesTheApproach(
            attackerIncapacitatedAfterApproach: true));
        Assert.True(CombatActionDispatch.ThrustSurvivesTheApproach(
            attackerIncapacitatedAfterApproach: false));
    }

    [Fact]
    public void AHealthyCharacterGuardsAndAHurtOneRests() {
        // One menu action, two behaviours, and the player is not told which they got.
        Assert.Equal(CombatActionDispatch.GuardAction.Defend, CombatActionDispatch.GuardFor(80));
        Assert.Equal(CombatActionDispatch.GuardAction.Defend, CombatActionDispatch.GuardFor(100));
        Assert.Equal(CombatActionDispatch.GuardAction.Rest, CombatActionDispatch.GuardFor(79));
    }

    [Fact]
    public void TheThresholdIsFourFifths() {
        Assert.Equal(80, CombatActionDispatch.DefendThresholdPercent);
    }

    [Fact]
    public void ClicksInTheMenuBarAreNotFieldActions() {
        Assert.True(CombatActionDispatch.ClickIsOnTheField(
            CombatActionDispatch.FieldBottomY - 1));
        Assert.False(CombatActionDispatch.ClickIsOnTheField(CombatActionDispatch.FieldBottomY));
    }

    [Fact]
    public void HandingControlOverSpendsThePreviousTurn() {
        Assert.True(CombatActionDispatch.SwitchingActorSpendsTheCurrentTurn);
    }
    [Fact]
    public void ANewRoundMakesEveryoneReadyAndClearsTheRoundBit() {
        CombatantFlags after = CombatActionDispatch.BeginRound(CombatantFlags.ClearedEachRound);
        Assert.Equal(CombatantFlags.Ready, after & CombatantFlags.Ready);
        Assert.Equal(CombatantFlags.None, after & CombatantFlags.ClearedEachRound);
    }

    [Fact]
    public void ButItDoesNotTouchParry() {
        // Parry is cleared when the defender is next picked, which is what makes Defend last
        // exactly one round rather than until the next boundary.
        CombatantFlags after = CombatActionDispatch.BeginRound(CombatantFlags.Parry);
        Assert.Equal(CombatantFlags.Parry, after & CombatantFlags.Parry);
    }

    [Fact]
    public void TheReadyBitIsTheOriginalsLowBit() {
        // Previously modelled as 0x04 — the bit the round reset clears.
        Assert.Equal(0x01, (int)CombatantFlags.Ready);
        Assert.Equal(0x02, (int)CombatantFlags.Dead);
        Assert.Equal(0x04, (int)CombatantFlags.ClearedEachRound);
        Assert.Equal(0x08, (int)CombatantFlags.Parry);
    }

    [Fact]
    public void ATargetThatHasFallenIsDroppedBetweenRounds() {
        Assert.False(CombatActionDispatch.KeepsTargetIntoNextRound(targetCanStillAct: false));
        Assert.True(CombatActionDispatch.KeepsTargetIntoNextRound(targetCanStillAct: true));
    }

    [Fact]
    public void EndingATurnFacesTheActorFirst() {
        Assert.True(CombatActionDispatch.TurnEndFacesBeforeSpending);
    }

    [Fact]
    public void AndTheAdvanceSkipsPastEveryoneWhoCannotAct() {
        Assert.True(CombatActionDispatch.AdvanceSkipsIncapacitatedActors);
    }
}
