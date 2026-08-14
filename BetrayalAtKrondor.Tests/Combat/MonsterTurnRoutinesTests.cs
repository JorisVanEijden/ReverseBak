namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The two bespoke creature turn routines. Both have a comparison that reads backwards from the
/// intuition, which is what makes them worth pinning.
/// </summary>
public class MonsterTurnRoutinesTests {
    [Fact]
    public void AnythingWithinReachIsJustHit() {
        Assert.Equal(MonsterMove.Melee, MonsterTurnRoutines.CloseOrRanged(1, 0).Move);
        Assert.Equal(MonsterMove.Melee, MonsterTurnRoutines.CloseOrRanged(0, 9).Move);
    }

    [Fact]
    public void ThisCreatureCastsMoreTheCloserItIsNotLess() {
        // roll >= distance, so the odds fall off with range — the opposite of "open at range,
        // close to melee". Swapping the comparison is an easy and invisible bug.
        Assert.Equal(MonsterMove.Cast, MonsterTurnRoutines.CloseOrRanged(2, 2).Move);
        Assert.Equal(MonsterMove.Shoot, MonsterTurnRoutines.CloseOrRanged(9, 8).Move);
    }

    [Fact]
    public void PastTenTilesItNeverCasts() {
        // The roll is [0,10), so no roll can reach a distance of 10.
        for (var roll = 0; roll < MonsterTurnRoutines.CloseRangeCastRollBound; roll++) {
            Assert.Equal(MonsterMove.Shoot, MonsterTurnRoutines.CloseOrRanged(10, roll).Move);
        }
    }

    [Fact]
    public void TheSimpleRoutineOnlyEverUsesTheOneSpellKind() {
        Assert.Equal(MonsterTurnRoutines.DefaultSpellKind,
            MonsterTurnRoutines.CloseOrRanged(2, 5).SpellKind);
    }

    [Fact]
    public void AfterWanderingItStillHitsWhateverItLandedNextTo() {
        Assert.Equal(MonsterMove.Melee, MonsterTurnRoutines.AfterWandering(1, 0, 5, true).Move);
    }

    [Fact]
    public void OneRollDrivesAllThreeOutcomes() {
        // Under 0x32 the alternate spell, 0x32..0x4F the default one, 0x50 and over defend.
        Assert.Equal(MonsterTurnRoutines.AlternateSpellKind,
            MonsterTurnRoutines.AfterWandering(4, 0x31, 5, true).SpellKind);
        Assert.Equal(MonsterTurnRoutines.DefaultSpellKind,
            MonsterTurnRoutines.AfterWandering(4, 0x32, 5, true).SpellKind);
        Assert.Equal(MonsterMove.Defend, MonsterTurnRoutines.AfterWandering(4, 0x50, 5, true).Move);
    }

    [Fact]
    public void AHalfStatOfExactlyOneNeverCasts() {
        // The guard is an inequality, not a minimum: reading it as "needs at least 1" would let the
        // weakest casters cast.
        Assert.Equal(MonsterMove.Defend, MonsterTurnRoutines.AfterWandering(4, 0, 1, true).Move);
        Assert.Equal(MonsterMove.Cast, MonsterTurnRoutines.AfterWandering(4, 0, 0, true).Move);
        Assert.Equal(MonsterMove.Cast, MonsterTurnRoutines.AfterWandering(4, 0, 2, true).Move);
    }

    [Fact]
    public void NoLineOfFireMeansNoCast() {
        Assert.Equal(MonsterMove.Defend, MonsterTurnRoutines.AfterWandering(4, 0, 5, false).Move);
    }

    [Fact]
    public void DefendingComesInTwoGrades() {
        // The brace is strictly above 0x32 while the alternate spell is strictly below it, so a
        // roll of exactly 0x32 does neither.
        Assert.True(MonsterTurnRoutines.AfterWandering(4, 0x60, 5, false).Braces);
        Assert.False(MonsterTurnRoutines.AfterWandering(4, 0x32, 5, false).Braces);
        Assert.False(MonsterTurnRoutines.AfterWandering(4, 0x10, 5, false).Braces);
    }

    [Fact]
    public void OnOurBuildAFlaggedCreatureWalksAndItsTurnEndsThere() {
        // The 1.02 CD release wraps the whole post-move decision in this check; the floppy build
        // attacks regardless, which would be a free action every turn.
        Assert.False(MonsterTurnRoutines.ActsAfterMoving(true));
        Assert.True(MonsterTurnRoutines.ActsAfterMoving(false));
    }

    [Fact]
    public void TheMeleeDamageBandIsInclusiveAtBothEnds() {
        // RNDR(lo, hi) is lo + rand % (hi - lo + 1).
        Assert.Equal(0x19, MonsterTurnRoutines.MeleeMinDamage);
        Assert.Equal(0x31, MonsterTurnRoutines.MeleeMaxDamage);
        // 25 through 49, so twenty-five distinct values.
        Assert.Equal(25, (MonsterTurnRoutines.MeleeMaxDamage - MonsterTurnRoutines.MeleeMinDamage) + 1);
    }
}
