namespace BetrayalAtKrondor.Tests.Combat;

using System.Collections.Generic;
using GameData.Resources.Combat;
using GameData.Resources.Spells;
using Xunit;

/// <summary>The AI's support turn — action slot 1, the only one that helps its own side.</summary>
public class MonsterHealTurnTests {
    private static readonly int[] Shipped = { 10, 10, 10, 0, 0, 0, 0, 0, 0 };

    [Fact]
    public void TheCasterNeedsHealthAboveTenToSpendTheTurnHelping() {
        // Threshold index 2, and strictly above. A caster on exactly 10 keeps its turn for itself.
        Assert.False(MonsterHealTurn.WellEnoughToHelp(10, Shipped));
        Assert.True(MonsterHealTurn.WellEnoughToHelp(11, Shipped));
    }

    [Fact]
    public void TheGateIndexIsItsOwn_NotSharedWithTheCastPasses() {
        // Three separate entries of the shipped ladder that happen to hold the same number. A
        // single shared constant would couple three knobs the data keeps apart.
        Assert.Equal(2, MonsterHealTurn.HealthGateThresholdIndex);
        Assert.NotEqual(MonsterCasterTurn.FirstPassThresholdIndex,
            MonsterHealTurn.HealthGateThresholdIndex);
        Assert.NotEqual(MonsterCasterTurn.RetryThresholdIndex,
            MonsterHealTurn.HealthGateThresholdIndex);
    }

    [Fact]
    public void TheDecisionReadsTheLastActorScanned_NotTheWeakest() {
        // *** THE ORIGINAL DEFECT, AND THE POINT OF THIS TYPE. *** The loop tracks a running
        // minimum and then reads the other slot. Implementing the evident intent would make
        // monster casters much better healers than the shipped game — most so with an ally nearly
        // dead, which is exactly when it shows.
        var health = new List<int> { 1, 99 };
        Assert.True(MonsterHealTurn.LastExamined(health, out int seen));
        Assert.Equal(99, seen);

        // Roll 50: the minimum (1) would fire the restore; the last (99) does not.
        Assert.NotEqual(MonsterHealTurn.RestoreSpell, MonsterHealTurn.ChooseSpell(
            health, roll: 50, restoreCastable: true, wardCastable: false,
            candidateAlreadyWarded: false));
    }

    [Fact]
    public void ALowLastReadingUnderTheRollTakesTheRestore() {
        Assert.Equal(SpellIds.GiftOfSung, MonsterHealTurn.ChooseSpell(
            new[] { 99, 1 }, roll: 50, restoreCastable: true, wardCastable: true,
            candidateAlreadyWarded: false));
    }

    [Fact]
    public void TheWardIsTheFallbackWhenTheRestoreIsNotCalledFor() {
        Assert.Equal(SpellIds.HochosHaven, MonsterHealTurn.ChooseSpell(
            new[] { 99 }, roll: 50, restoreCastable: true, wardCastable: true,
            candidateAlreadyWarded: false));
    }

    [Fact]
    public void AnUncastableRestoreFallsThroughToTheWardRatherThanEndingTheTurn() {
        Assert.Equal(SpellIds.HochosHaven, MonsterHealTurn.ChooseSpell(
            new[] { 1 }, roll: 50, restoreCastable: false, wardCastable: true,
            candidateAlreadyWarded: false));
    }

    [Fact]
    public void AProbeThatAlreadyCarriesTheWardKillsTheWholeTurn() {
        // And it is the PROBE, not the recipient — see WardChecksTheProbeNotTheRecipient.
        Assert.True(MonsterHealTurn.WardChecksTheProbeNotTheRecipient);
        Assert.Equal(MonsterHealTurn.NoSpell, MonsterHealTurn.ChooseSpell(
            new[] { 99 }, roll: 50, restoreCastable: true, wardCastable: true,
            candidateAlreadyWarded: true));
    }

    [Fact]
    public void AnEmptyOpposingListSkipsTheRestoreRatherThanGuessing() {
        // The original reads an uninitialised slot here; there is no faithful answer, so this
        // takes the safe half. Unreachable in a real fight — the opposing side is non-empty for
        // as long as there is a fight.
        Assert.False(MonsterHealTurn.LastExamined(new int[0], out _));
        Assert.Equal(SpellIds.HochosHaven, MonsterHealTurn.ChooseSpell(
            new int[0], roll: 79, restoreCastable: true, wardCastable: true,
            candidateAlreadyWarded: false));
    }

    [Fact]
    public void OnlyAWoundedLivingAllyCanReceiveIt() {
        Assert.False(MonsterHealTurn.CanReceive(0, isCaster: false), "a corpse");
        Assert.False(MonsterHealTurn.CanReceive(100, isCaster: false), "untouched");
        Assert.False(MonsterHealTurn.CanReceive(50, isCaster: true), "never itself");
        Assert.True(MonsterHealTurn.CanReceive(99, isCaster: false));
    }

    [Fact]
    public void OneAllyPerAction_TheScanStopsAtTheFirstEligible() {
        // A caster cannot blanket its pack in one turn however many are hurt.
        Assert.Equal(1, MonsterHealTurn.PickRecipient(new[] { 100, 40, 20 }, casterIndex: -1));
    }

    [Fact]
    public void TheCasterIsSkippedEvenWhenItIsTheWorstOff() {
        Assert.Equal(2, MonsterHealTurn.PickRecipient(new[] { 100, 5, 60 }, casterIndex: 1));
        Assert.Equal(-1, MonsterHealTurn.PickRecipient(new[] { 100, 5 }, casterIndex: 1));
    }
}
