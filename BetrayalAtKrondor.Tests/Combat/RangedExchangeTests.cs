namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>One ranged shot end to end.</summary>
public class RangedExchangeTests {
    [Fact]
    public void TheCrossbowWearsEvenWhenTheShotMisses() {
        // *** The asymmetry a port drops. *** The attacker's wear call is the routine's LAST
        // statement, outside the hit branch; the target's is inside it. Putting both inside
        // if (hit) would make missing free.
        Assert.True(RangedExchange.WeaponWearsEvenOnAMiss);
        Assert.True(RangedExchange.ArmourWearsOnlyOnAHit);
        Assert.Equal(2, RangedExchange.ShooterWearCategory);   // the crossbow
        Assert.Equal(4, RangedExchange.TargetWearCategory);    // armour
    }

    [Fact]
    public void MostKindsKnockBackOne_AndThreeKindsKnockBackTwo() {
        // Reading the switch as "some kinds are special, the rest do nothing" would remove knockback
        // from every ordinary shot.
        foreach (int kind in new[] { 0, 1, 2, 3, 7, 8, 9 }) {
            Assert.Equal(1, RangedExchange.KnockbackFor(kind));
        }
        foreach (int kind in new[] { 4, 5, 6 }) {
            Assert.Equal(2, RangedExchange.KnockbackFor(kind));
        }
    }

    [Fact]
    public void TheHeavierKindsAlsoSetTheLowDamageFlag() {
        Assert.Equal(RangedExchange.BaseDamageFlags | 1, RangedExchange.DamageFlagsFor(5));
        Assert.Equal(RangedExchange.BaseDamageFlags, RangedExchange.DamageFlagsFor(7));
        Assert.Equal(0x540, RangedExchange.BaseDamageFlags);
    }

    [Fact]
    public void KindThreeAppliesAStatusEffectAndFlagsItSeparately() {
        Assert.True(RangedExchange.AppliesStatusEffect(3));
        Assert.False(RangedExchange.AppliesStatusEffect(4));
        Assert.Equal(RangedExchange.BaseDamageFlags | 8, RangedExchange.DamageFlagsFor(3));
        // and it is one of the knockback-1 kinds, not a heavy one
        Assert.Equal(1, RangedExchange.KnockbackFor(3));
    }

    [Fact]
    public void CrossbowSkillIsPaidTwiceOnAHit_OnceOnAMiss() {
        // Same shape as melee: paid on declaration, and again on connecting.
        Assert.Equal(1, RangedExchange.SkillAwards(hit: false));
        Assert.Equal(2, RangedExchange.SkillAwards(hit: true));
    }

    [Fact]
    public void WhatTheShooterWearsChangesTheirAccuracy() {
        // The check is Crossbow + an armour-derived term, not Crossbow alone.
        Assert.Equal(37, RangedExchange.EffectiveSkill(crossbowSkill: 40, armourModifier: -3));
    }
}
