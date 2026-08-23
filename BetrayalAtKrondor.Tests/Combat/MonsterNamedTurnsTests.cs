namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Two AI routines whose canassa names describe the wrong action. Both are pinned here precisely
/// because the names would mislead a reader who did not open the bodies.
/// </summary>
public class MonsterNamedTurnsTests {
    [Fact]
    public void TheMELEERoutineActuallyShootsWhenTheTargetIsNotAdjacent() {
        // *** combataiact_actor_melee_attack's main branch is RANGED. *** Trusting the name would
        // have a port swinging at a target three tiles away.
        Assert.Equal(MonsterMeleeTurn.Outcome.RangedAttack,
            MonsterMeleeTurn.Choose(hasTarget: true, hasLineOfSight: true, distance: 3));
    }

    [Fact]
    public void ItFallsBackToTileOrAttackWhenAdjacentOrBlocked() {
        Assert.Equal(MonsterMeleeTurn.Outcome.TileOrAttack,
            MonsterMeleeTurn.Choose(true, hasLineOfSight: true, distance: 1));
        Assert.Equal(MonsterMeleeTurn.Outcome.TileOrAttack,
            MonsterMeleeTurn.Choose(true, hasLineOfSight: false, distance: 5));
    }

    [Fact]
    public void ANullTargetIsGuardedInTheCDBuild() {
        Assert.Equal(MonsterMeleeTurn.Outcome.NoTarget,
            MonsterMeleeTurn.Choose(hasTarget: false, hasLineOfSight: true, distance: 4));
        Assert.True(MonsterMeleeTurn.NullTargetIsGuarded);
    }

    [Fact]
    public void TheKnockbackIsAnimatedBEFORETheDamageLands() {
        // The routine steps knockbackFrame 1..4 presenting frames, THEN applies damage. Applying
        // damage first would kill the target before the animation it is meant to play.
        Assert.Equal(4, MonsterMeleeTurn.KnockbackFrames);
        Assert.Equal((0xf, 0x22), MonsterMeleeTurn.Damage);
    }

    [Fact]
    public void TheCHARGERoutineDoesNotCharge_ItShoots() {
        Assert.Equal(MonsterChargeTurn.Outcome.RangedAttack,
            MonsterChargeTurn.Choose(distance: 5, hasLineOfSight: true, roll: 50));
    }

    [Fact]
    public void ItWillNotShootACloseTarget_ThreeTilesIsTheMinimum() {
        // A wider dead zone than the usual "not adjacent" rule: two tiles paths instead of shooting.
        Assert.Equal(MonsterChargeTurn.Outcome.Path,
            MonsterChargeTurn.Choose(distance: 2, hasLineOfSight: true, roll: 50));
        Assert.Equal(MonsterChargeTurn.Outcome.RangedAttack,
            MonsterChargeTurn.Choose(distance: 3, hasLineOfSight: true, roll: 50));
    }

    [Fact]
    public void TheFivePercentIsAFAILURERate_NotASuccessRate() {
        // RND(100) >= 5 shoots, so it fires 95 times in 100. Reading 5 as the success rate would
        // invert the routine almost completely.
        Assert.Equal(MonsterChargeTurn.Outcome.Path,
            MonsterChargeTurn.Choose(distance: 5, hasLineOfSight: true, roll: 4));
        Assert.Equal(MonsterChargeTurn.Outcome.RangedAttack,
            MonsterChargeTurn.Choose(distance: 5, hasLineOfSight: true, roll: 5));
    }

    [Fact]
    public void TheChargeRoutineNeverKeepsATarget() {
        Assert.True(MonsterChargeTurn.ClearsStoredTarget);
        Assert.Equal(0, MonsterChargeTurn.TraceMode);   // and it traces in mode 0, unlike its siblings
    }
}
