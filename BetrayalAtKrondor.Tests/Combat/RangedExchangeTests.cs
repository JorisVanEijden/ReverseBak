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
    public void TheStatusEffectOnKindThreeIsARenderingHack_NotAGameplayEffect() {
        // Identified 2026-08-22: type 4 is never queried anywhere (find_type is called with 1, 3, 6,
        // 0xd, 0x17, 0x1f - never 4), the expiry switch handles only type 1, and the add/remove pair
        // brackets the PARTICLE BURST rather than the damage. Its only job is to make statusHead
        // non-empty while worldfx renders. So AppliesStatusEffect is about the visual, and a port
        // that implements "effect 4" as a status is implementing nothing.
        Assert.True(RangedExchange.AppliesStatusEffect(RangedExchange.StatusEffectQuarrelKind));
        Assert.Equal(3, RangedExchange.StatusEffectQuarrelKind);

        // It still carries its own damage flag, which IS real.
        Assert.Equal(RangedExchange.BaseDamageFlags | 8,
            RangedExchange.DamageFlagsFor(RangedExchange.StatusEffectQuarrelKind));
    }

    [Fact]
    public void CrossbowSkillIsPaidTwiceOnAHit_OnceOnAMiss() {
        // Same shape as melee: paid on declaration, and again on connecting.
        Assert.Equal(1, RangedExchange.SkillAwards(hit: false));
        Assert.Equal(2, RangedExchange.SkillAwards(hit: true));
    }

    [Fact]
    public void THEWEAPONChangesTheShootersAccuracy_notWhatTheyAreWEARING() {
        // *** CORRECTED 2026-08-31. *** This asserted `armourModifier: -3` — a negative,
        // armour-derived PENALTY — from the helper's name, combataiturn_armor_eff_stat. The body
        // has no armour in it: cbstat_find_intact_equip_cat(actor, 2) is the CROSSBOW, and the
        // return is that bow's accuracy scaled by its condition. It is a bonus, and it applies to
        // every shooter holding one.
        //
        // The arithmetic the old test pinned is unchanged — it is still a sum — which is why the
        // wrong reading survived: nothing about `40 + x` says what x is.
        Assert.Equal(40 + 27, RangedExchange.EffectiveSkill(
            crossbowSkill: 40, weaponTerm: RangedExchange.WeaponTerm(bowAccuracy: 30, conditionPercent: 90)));
    }
}
