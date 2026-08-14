namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The three creature routines that shoot when they can and hand the turn back when they cannot.
/// Each carries one line that is easy to drop and changes the creature materially.
/// </summary>
public class MonsterDeferringRoutinesTests {
    [Fact]
    public void TheDistantShotNeedsMoreRoomThanTheOtherRoutinesWant() {
        Assert.False(MonsterTurnRoutines.TakesTheDistantShot(2, true, 99));
        Assert.True(MonsterTurnRoutines.TakesTheDistantShot(3, true, 99));
    }

    [Fact]
    public void ItPassesUpTheShotOnARollUnderFive() {
        // A 5% flinch, small enough to look like a rounding artefact and easy to drop.
        Assert.False(MonsterTurnRoutines.TakesTheDistantShot(9, true, 0));
        Assert.False(MonsterTurnRoutines.TakesTheDistantShot(9, true, 4));
        Assert.True(MonsterTurnRoutines.TakesTheDistantShot(9, true, 5));
    }

    [Fact]
    public void NoLineOfFireStillMeansNoShot() {
        Assert.False(MonsterTurnRoutines.TakesTheDistantShot(9, false, 99));
    }

    [Fact]
    public void ThatRoutineDropsItsTargetEveryTurnWhateverItDid() {
        // So it always reads as disengaged to the target filters: never findable by the "engaged"
        // role, always eligible for the "disengaged" one.
        Assert.True(MonsterTurnRoutines.ClearsTargetAfterActing);
    }

    [Fact]
    public void TheThreeAttacksAreChosenFlatWithNoPreference() {
        Assert.Equal(2, MonsterTurnRoutines.MixedAttack(0).ActionId);
        Assert.Equal(3, MonsterTurnRoutines.MixedAttack(1).ActionId);
        Assert.Equal(4, MonsterTurnRoutines.MixedAttack(2).ActionId);
    }

    [Fact]
    public void TheKnockbackRunsOppositeToTheDamage() {
        // The hardest of the three shoves least and the weakest shoves most, so they are not three
        // strengths of one attack.
        MonsterTurnRoutines.RangedTurn hardest = MonsterTurnRoutines.MixedAttack(0);
        MonsterTurnRoutines.RangedTurn weakest = MonsterTurnRoutines.MixedAttack(2);

        Assert.True(hardest.MaxDamage > weakest.MaxDamage);
        Assert.True(hardest.KnockbackFrames < weakest.KnockbackFrames);
    }

    [Fact]
    public void AllThreeScaleWithTheAttackersStateUnlikeEveryOtherRoutine() {
        for (var roll = 0; roll < MonsterTurnRoutines.MixedAttackRollBound; roll++) {
            Assert.True(MonsterTurnRoutines.MixedAttack(roll).ScalesWithStat);
        }
        Assert.False(MonsterTurnRoutines.LightShot().ScalesWithStat);
        Assert.False(MonsterTurnRoutines.HeavyBolt().ScalesWithStat);
    }

    [Fact]
    public void TheThreeAttackRoutineWantsMoreRoomThanTheBoltOne() {
        Assert.False(MonsterTurnRoutines.TakesTheMixedAttack(2, true));
        Assert.True(MonsterTurnRoutines.TakesTheMixedAttack(3, true));

        Assert.True(MonsterTurnRoutines.TakesTheHeavyBolt(2, true));
        Assert.False(MonsterTurnRoutines.TakesTheHeavyBolt(1, true));
    }

    [Fact]
    public void TheBoltIsTheHardestSingleAttackInTheBespokeSet() {
        MonsterTurnRoutines.RangedTurn bolt = MonsterTurnRoutines.HeavyBolt();

        Assert.Equal(0x2d, bolt.MinDamage);
        Assert.Equal(0x4a, bolt.MaxDamage);
        Assert.Equal(4, bolt.KnockbackFrames);
        Assert.True(bolt.MinDamage > MonsterTurnRoutines.VolleyMaxDamage);
    }

    [Fact]
    public void TheBoltCreatureRefillsAStatBeforeDecidingAnything() {
        // One assignment at the top of the routine: whatever drains that stat is undone every turn,
        // so the creature cannot be worn down through it at all.
        Assert.True(MonsterTurnRoutines.RefillsStatEachTurn);
    }

    [Fact]
    public void BlockedOrTooCloseAllThreeHandTheTurnBack() {
        Assert.False(MonsterTurnRoutines.TakesTheMixedAttack(9, false));
        Assert.False(MonsterTurnRoutines.TakesTheHeavyBolt(9, false));
        Assert.False(MonsterTurnRoutines.TakesTheDistantShot(9, false, 99));
    }
}
