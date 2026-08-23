namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>The creature-specific ranged/breath turn.</summary>
public class MonsterRangedTurnTests {
    [Fact]
    public void ACLEARSHOTIsNotEnough_ItAlsoNeedsTheRoll() {
        // The creature walks away half the time even with line of sight. Attacking whenever it can
        // see the target makes these creatures twice as aggressive as the original.
        Assert.Equal(MonsterRangedTurn.Outcome.Path,
            MonsterRangedTurn.Choose(hasLineOfSight: true, pathRoll: 49, tierRoll: 0, creatureType: 0x29));
        Assert.Equal(MonsterRangedTurn.Outcome.Heavy,
            MonsterRangedTurn.Choose(hasLineOfSight: true, pathRoll: 50, tierRoll: 0, creatureType: 0x29));
    }

    [Fact]
    public void NoLineOfSightMeansNoAttackAtAll() {
        Assert.Equal(MonsterRangedTurn.Outcome.Path,
            MonsterRangedTurn.Choose(hasLineOfSight: false, pathRoll: 99, tierRoll: 0, creatureType: 0x29));
    }

    [Fact]
    public void TheHEAVYTierIsTheCommonOne() {
        // *** RND2(4) <= 2 is three outcomes in four. *** Reading "<= 2" as a minority case and
        // swapping the branches would cut these creatures from 20-28 damage to 4-7 most turns.
        for (var tier = 0; tier <= 2; tier++) {
            Assert.Equal(MonsterRangedTurn.Outcome.Heavy,
                MonsterRangedTurn.Choose(true, pathRoll: 80, tierRoll: tier, creatureType: 0x29));
        }
        Assert.Equal(MonsterRangedTurn.Outcome.Weak,
            MonsterRangedTurn.Choose(true, pathRoll: 80, tierRoll: 3, creatureType: 0x29));
    }

    [Fact]
    public void CreatureThirtyNineNeverUsesTheWeakFallback() {
        Assert.Equal(MonsterRangedTurn.Outcome.Heavy,
            MonsterRangedTurn.Choose(true, pathRoll: 80, tierRoll: 3,
                creatureType: MonsterRangedTurn.AlwaysHeavyCreature));
    }

    [Fact]
    public void EachCreatureHasItsOwnActionIdAndKnockback() {
        Assert.Equal((2, 1), MonsterRangedTurn.HeavyByCreature[0x29]);
        Assert.Equal((3, 3), MonsterRangedTurn.HeavyByCreature[0x2a]);
        Assert.Equal((0x32, 3), MonsterRangedTurn.HeavyByCreature[0x2b]);
        Assert.Equal((0x32, 3), MonsterRangedTurn.HeavyByCreature[0x39]);
    }

    [Fact]
    public void OnlyFourCreaturesHaveAHeavyAttack_TheOriginalDoesNotGuardTheRest() {
        // The switch has no default, so an unlisted creature reaching the heavy branch would attack
        // with an UNINITIALISED action id and knockback. A latent bug, not a rule - recorded so a
        // port does not reproduce garbage, and does not silently invent a default either.
        Assert.Equal(4, MonsterRangedTurn.HeavyByCreature.Count);
        Assert.False(MonsterRangedTurn.HasHeavyAttack(0x01));
    }

    [Fact]
    public void TheTwoDamageTiersAreFarApart() {
        Assert.Equal((0x14, 0x1d), MonsterRangedTurn.HeavyDamage);
        Assert.Equal((4, 8), MonsterRangedTurn.WeakDamage);
        Assert.True(MonsterRangedTurn.HeavyDamage.Min > MonsterRangedTurn.WeakDamage.Max);
    }
}
