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

    [Fact]
    public void HelpIsAddressedByMenuPositionNotByTheActionId() {
        // *** The trap this mapping exists for. *** The ids skip 7 and come back to it, while the
        // help records are unbroken — so any arithmetic on the id is right for the first five
        // entries and wrong from the sixth on. Spelled out rather than looped, because the whole
        // point is that the sequence is irregular.
        Assert.Equal(0xFE, CombatActionDispatch.HelpRecordFor(2));
        Assert.Equal(0xFF, CombatActionDispatch.HelpRecordFor(3));
        Assert.Equal(0x100, CombatActionDispatch.HelpRecordFor(4));
        Assert.Equal(0x101, CombatActionDispatch.HelpRecordFor(5));
        Assert.Equal(0x102, CombatActionDispatch.HelpRecordFor(6));
        Assert.Equal(0x103, CombatActionDispatch.HelpRecordFor(8));   // id 8, not id 7
        Assert.Equal(0x104, CombatActionDispatch.HelpRecordFor(9));
        Assert.Equal(0x105, CombatActionDispatch.HelpRecordFor(7));   // 7 comes back LAST
        Assert.Equal(0x106, CombatActionDispatch.HelpRecordFor(50));
        Assert.Equal(0x10D, CombatActionDispatch.HelpRecordFor(33));  // and the run ends unbroken
    }

    [Fact]
    public void IdArithmeticWouldGetItWrong_WhichIsWhyThereIsAMapping() {
        // If help were HelpRecordBase + (id - 2), id 8 would give 0x104 and id 7 would give 0x103.
        // Both are the other one's record — a swap that looks plausible on screen.
        Assert.NotEqual(CombatActionDispatch.HelpRecordBase + (8 - 2), CombatActionDispatch.HelpRecordFor(8));
        Assert.NotEqual(CombatActionDispatch.HelpRecordBase + (7 - 2), CombatActionDispatch.HelpRecordFor(7));
        Assert.Equal(CombatActionDispatch.HelpRecordFor(7), CombatActionDispatch.HelpRecordBase + (8 - 2) + 1);
    }

    [Fact]
    public void TheFirstEightMenuEntriesAreActorCommandsAndTheRestAreNot() {
        Assert.Equal(0, CombatActionDispatch.ActorCommandFor(2));
        Assert.Equal(6, CombatActionDispatch.ActorCommandFor(9));
        Assert.Equal(7, CombatActionDispatch.ActorCommandFor(7));
        // 50 flips the menu page; it is a control, not something the actor does.
        Assert.Equal(-1, CombatActionDispatch.ActorCommandFor(50));
        Assert.Equal(-1, CombatActionDispatch.ActorCommandFor(33));
        Assert.Equal(CombatActionDispatch.ActorCommandCount,
            CombatActionDispatch.MenuActionIds.Length - 8);
    }

    [Fact]
    public void AnIdThatIsNotOnTheMenuHasNoPositionAndNoHelp() {
        Assert.Equal(-1, CombatActionDispatch.MenuPositionOf(1));
        Assert.Equal(-1, CombatActionDispatch.HelpRecordFor(1));
        Assert.Equal(-1, CombatActionDispatch.ActorCommandFor(1));
    }

    [Fact]
    public void EveryMenuIdIsDistinctAndTheHelpRunIsUnbroken() {
        var seen = new System.Collections.Generic.HashSet<int>();
        for (var i = 0; i < CombatActionDispatch.MenuActionIds.Length; i++) {
            Assert.True(seen.Add(CombatActionDispatch.MenuActionIds[i]), "duplicate action id");
            Assert.Equal(CombatActionDispatch.HelpRecordBase + i,
                CombatActionDispatch.HelpRecordFor(CombatActionDispatch.MenuActionIds[i]));
        }
    }

    [Fact]
    public void ALeftClickOnlyStoresTheChoice() {
        // The dispatcher records selectedMenuCmd and returns; acting on it happens afterwards.
        // Performing the action in the click handler runs it a step too early.
        Assert.True(CombatActionDispatch.LeftClickOnlyRecordsTheChoice);
    }
}
