namespace BetrayalAtKrondor.Tests.Combat;

using GameData;
using GameData.Resources.Combat;
using Xunit;

/// <summary>The variant-attack routine and the heavy ranged routine.</summary>
public class MonsterVariantAndHeavyTurnTests {
    [Fact]
    public void TheVariantRoutineNeedsMoreRoomThanItsSiblings() {
        // > 2, where every other attacking routine here uses > 1. Copying a neighbour's bound would
        // make this creature attack from a range it never uses.
        Assert.False(MonsterVariantAttackTurn.Attacks(hasLineOfSight: true, distance: 2));
        Assert.True(MonsterVariantAttackTurn.Attacks(hasLineOfSight: true, distance: 3));
    }

    [Fact]
    public void DamageAndKnockbackMoveInOPPOSITEDirections() {
        // *** Not a weak/medium/strong ladder. *** Variant 0 hits hardest and shoves least; variant 2
        // hits weakest and shoves most. Ranking them as tiers gets the knockback backwards.
        MonsterVariantAttackTurn.Variant hardest = MonsterVariantAttackTurn.VariantFor(0);
        MonsterVariantAttackTurn.Variant weakest = MonsterVariantAttackTurn.VariantFor(2);

        Assert.True(hardest.DamageMin > weakest.DamageMin);
        Assert.True(hardest.Knockback < weakest.Knockback);
        Assert.Equal(3, MonsterVariantAttackTurn.Variants.Length);
    }

    [Fact]
    public void TheVariantDamageIsStatScaled_SoTheRangesAreInputsNotOutputs() {
        Assert.True(MonsterVariantAttackTurn.DamageIsStatScaled);
    }

    [Fact]
    public void TheHeavyRoutineRestoresSTRENGTHEveryTurn() {
        // *** The first statement of the routine, before it even looks for a target. *** Draining
        // this creature's Strength is pointless - it undoes the damage itself each turn, whether or
        // not it goes on to attack. Easy to skim past as bookkeeping.
        Assert.True(MonsterHeavyRangedTurn.RestoresStrengthEachTurn);
        Assert.Equal(ActorAttribute.Strength, MonsterHeavyRangedTurn.RestoredAttribute);
        Assert.Equal(3, (int)MonsterHeavyRangedTurn.RestoredAttribute);   // stats[3]
    }

    [Fact]
    public void TheHeavyRoutineHitsHardestInTheFile() {
        Assert.Equal((0x2d, 0x4a), MonsterHeavyRangedTurn.Damage);
        Assert.True(MonsterHeavyRangedTurn.Damage.Min
            > MonsterTurnRoutines.HeavyShotFor(0x29).Value.MaxDamage);
        Assert.Equal(4, MonsterHeavyRangedTurn.Knockback);
    }

    [Fact]
    public void TwoRoutinesWithNearlyTheSameNameAreVeryDifferentAttacks() {
        // combataiact_ranged_attack vs combataiact_ranged_attack_TURN. 45-73 against 20-28, and
        // different range rules - so the names cannot be relied on to tell them apart.
        MonsterTurnRoutines.RangedTurn spit = MonsterTurnRoutines.HeavyShotFor(0x29).Value;
        Assert.NotEqual(MonsterHeavyRangedTurn.Damage, (spit.MinDamage, spit.MaxDamage));
        Assert.True(MonsterHeavyRangedTurn.Attacks(hasLineOfSight: true, distance: 2));
    }
}
