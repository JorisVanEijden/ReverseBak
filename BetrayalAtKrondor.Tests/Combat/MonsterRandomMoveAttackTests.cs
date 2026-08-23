namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>The wander-then-act routine's decision.</summary>
public class MonsterRandomMoveAttackTests {
    [Fact]
    public void AdjacentIsMeleeRegardlessOfEverythingElse() {
        Assert.Equal(MonsterRandomMoveAttack.Action.Melee,
            MonsterRandomMoveAttack.Choose(distance: 1, roll: 99, halfHealth: 1, hasLineOfSight: false));
    }

    [Fact]
    public void CastingNeedsFOURConditionsAtOnce() {
        // Not adjacent, roll under 80, line of sight, and halfHealth != 1. Drop any one and it rests.
        Assert.Equal(MonsterRandomMoveAttack.Action.CastFive,
            MonsterRandomMoveAttack.Choose(4, roll: 10, halfHealth: 20, hasLineOfSight: true));

        Assert.NotEqual(MonsterRandomMoveAttack.Action.CastFive,
            MonsterRandomMoveAttack.Choose(4, roll: 10, halfHealth: 20, hasLineOfSight: false));
        Assert.NotEqual(MonsterRandomMoveAttack.Action.CastFive,
            MonsterRandomMoveAttack.Choose(4, roll: 90, halfHealth: 20, hasLineOfSight: true));
    }

    [Fact]
    public void HalfHealthOfExactlyOneBlocksCasting_ButZeroDoesNot() {
        // *** Peculiar but real. *** A monster on 2-3 health cannot cast; one on 0-1 (half = 0) can.
        // Reproduced rather than tidied into "must be healthy enough", which would be a different
        // rule.
        Assert.Equal(MonsterRandomMoveAttack.Action.Rest,
            MonsterRandomMoveAttack.Choose(4, roll: 10, halfHealth: 1, hasLineOfSight: true));
        Assert.Equal(MonsterRandomMoveAttack.Action.CastFive,
            MonsterRandomMoveAttack.Choose(4, roll: 10, halfHealth: 0, hasLineOfSight: true));
    }

    [Fact]
    public void TheRollAlsoPicksWHICHSpellTargetingType() {
        Assert.Equal(MonsterRandomMoveAttack.Action.CastFive,
            MonsterRandomMoveAttack.Choose(4, roll: 49, halfHealth: 20, hasLineOfSight: true));
        Assert.Equal(MonsterRandomMoveAttack.Action.CastFour,
            MonsterRandomMoveAttack.Choose(4, roll: 50, halfHealth: 20, hasLineOfSight: true));
    }

    [Fact]
    public void TheFallbackIsRest_AndHalfTheTimeDefendAsWell() {
        // The two are NOT exclusive: the original rests, then additionally raises a guard on a high
        // roll, so the monster holds both flags. Treating them as either/or drops the guard.
        Assert.Equal(MonsterRandomMoveAttack.Action.Rest,
            MonsterRandomMoveAttack.Choose(4, roll: 20, halfHealth: 1, hasLineOfSight: true));
        // Exactly 50 does NOT defend: the original tests > not >=.
        Assert.Equal(MonsterRandomMoveAttack.Action.Rest,
            MonsterRandomMoveAttack.Choose(4, roll: MonsterRandomMoveAttack.MidRoll,
                halfHealth: 1, hasLineOfSight: true));
        Assert.Equal(MonsterRandomMoveAttack.Action.RestAndDefend,
            MonsterRandomMoveAttack.Choose(4, roll: 90, halfHealth: 20, hasLineOfSight: false));
    }

    [Fact]
    public void AWoundedCasterCastsWeaker() {
        // The same halved health that gates the decision is the spell's magnitude.
        Assert.Equal(20, MonsterRandomMoveAttack.SpellMagnitude(20));
        Assert.Equal(3, MonsterRandomMoveAttack.SpellMagnitude(3));
    }
}
