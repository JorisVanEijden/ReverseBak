namespace BetrayalAtKrondor.Tests.Combat;

using GameData;
using GameData.Resources.Combat;
using System;
using Xunit;

/// <summary>
/// Combat arithmetic (CBSTAT.C / CBENC.C / CACTOR.C / COMBAT.C). The cases below pin the parts a
/// port gets wrong by tidying: tiers that overwrite instead of stacking, the parry penalty applied to
/// the roll rather than the chance, armour that reads only the first equipped piece, and the
/// weakness/resistance pair that canassa names backwards.
/// </summary>
public class CombatFormulasTests {
    private static Func<int, int> Rolls(params int[] results) {
        var i = 0;
        return _ => results[i++];
    }

    private static Func<int, int> Never() => _ => throw new InvalidOperationException("must not roll");

    // ---- blessing ------------------------------------------------------------------------

    [Theory]
    [InlineData(ItemFlags.Blessed1, 105)]
    [InlineData(ItemFlags.Blessed2, 110)]
    [InlineData(ItemFlags.Blessed3, 115)]
    public void ABlessedItemLiftsTheValueByItsTier(ItemFlags flag, int expected) {
        Assert.Equal(expected, CombatFormulas.ApplyEquippedBlessing(100, flag));
    }

    [Fact]
    public void BlessingTiersDoNotStackTheHighestSimplyWins() {
        // Each tier assigns from the original value, so this is 115% and not 105% * 110% * 115%.
        int actual = CombatFormulas.ApplyEquippedBlessing(
            100, ItemFlags.Blessed1 | ItemFlags.Blessed2 | ItemFlags.Blessed3);

        Assert.Equal(115, actual);
    }

    [Fact]
    public void AnUnblessedItemChangesNothing() {
        Assert.Equal(100, CombatFormulas.ApplyEquippedBlessing(100, 0));
    }

    // ---- defence -------------------------------------------------------------------------

    [Fact]
    public void DefenceContributesAQuarterOfTheStat() {
        Assert.Equal(15, CombatFormulas.DefenseRating(defense: 60, canAct: true, 0));
    }

    [Fact]
    public void AnActorThatCannotActDefendsNotAtAll() {
        // Stunned or asleep: trivially hittable, whatever its Defense says.
        Assert.Equal(0, CombatFormulas.DefenseRating(defense: 200, canAct: false, ItemFlags.Blessed3));
    }

    [Fact]
    public void DefenceRatingCannotActuallyReachItsOwnCap() {
        // The original clamps this to 98, but Defense is a byte: the best case is 255>>2 = 63 blessed
        // to 72, so the clamp is unreachable and defence alone can never push melee to the 2% floor.
        // Kept faithfully anyway — an override raising the stat width would need it.
        Assert.Equal(72, CombatFormulas.DefenseRating(defense: 255, canAct: true, ItemFlags.Blessed3));
    }

    // ---- armour --------------------------------------------------------------------------

    [Fact]
    public void UnarmouredMitigationIsJustAQuarterOfDefence() {
        Assert.Equal(10, CombatFormulas.ArmorRating(
            defense: 40, hasArmorEquipped: false, armorConditionPercent: 0, armorRating: 0, classGroupModifier: 0));
    }

    [Fact]
    public void WornArmourAddsItsConditionScaledRating() {
        // 40>>2 = 10, plus 50% condition of a 20-rating piece = 10, total 20.
        Assert.Equal(20, CombatFormulas.ArmorRating(
            defense: 40, hasArmorEquipped: true, armorConditionPercent: 50, armorRating: 20, classGroupModifier: 0));
    }

    [Fact]
    public void TheClassModifierMultipliesTheWholeRatingRatherThanBeingAddedToIt() {
        // (10 + 10) * 150/100 = 30. Adding the modifier would give 70.
        Assert.Equal(30, CombatFormulas.ArmorRating(
            defense: 40, hasArmorEquipped: true, armorConditionPercent: 50, armorRating: 20, classGroupModifier: 50));
    }

    [Fact]
    public void ArmourMitigationIsCappedAt98SoSomethingAlwaysGetsThrough() {
        Assert.Equal(98, CombatFormulas.ArmorRating(
            defense: 255, hasArmorEquipped: true, armorConditionPercent: 100, armorRating: 200, classGroupModifier: 0));
    }

    // ---- melee to-hit --------------------------------------------------------------------

    [Fact]
    public void TheWeaponTermIsScaledByClassAffinityThenByCondition() {
        // weapon 40 * 150/100 = 60, * 50/100 = 30, + accuracy 40 = 70.
        int chance = CombatFormulas.MeleeHitChance(
            accuracyMelee: 40, hasWeapon: true, weaponAccuracy: 40, classGroupModifier: 50,
            weaponConditionPercent: 50, weaponFlags: 0, targetDefenseRating: 0);

        Assert.Equal(70, chance);
    }

    [Fact]
    public void AnUnarmedAttackerGetsNoWeaponTermAtAll() {
        int chance = CombatFormulas.MeleeHitChance(
            accuracyMelee: 40, hasWeapon: false, weaponAccuracy: 99, classGroupModifier: 99,
            weaponConditionPercent: 99, weaponFlags: ItemFlags.Blessed3, targetDefenseRating: 0);

        Assert.Equal(40, chance);
    }

    [Fact]
    public void TheTargetsDefenceComesStraightOffTheChance() {
        int chance = CombatFormulas.MeleeHitChance(
            accuracyMelee: 50, hasWeapon: false, weaponAccuracy: 0, classGroupModifier: 0,
            weaponConditionPercent: 0, weaponFlags: 0, targetDefenseRating: 20);

        Assert.Equal(30, chance);
    }

    [Fact]
    public void HitChanceIsHeldInsideTheTwoToNinetyEightBand() {
        Assert.Equal(2, CombatFormulas.MeleeHitChance(0, false, 0, 0, 0, 0, targetDefenseRating: 500));
        Assert.Equal(98, CombatFormulas.MeleeHitChance(500, false, 0, 0, 0, 0, targetDefenseRating: 0));
    }

    [Fact]
    public void ParryingPushesTheRollUpRatherThanTheChanceDown() {
        // The distinction matters: at the 98 ceiling there is no headroom left to subtract from, so a
        // penalty applied to the chance would do nothing at all. Applied to the roll it still bites.
        Assert.True(CombatFormulas.MeleeHits(roll: 90, hitChance: 98, targetParrying: false));
        Assert.False(CombatFormulas.MeleeHits(roll: 90, hitChance: 98, targetParrying: true));
    }

    // ---- ranged to-hit -------------------------------------------------------------------

    [Fact]
    public void AnAdjacentShotPaysNoDistancePenalty() {
        Assert.Equal(50, CombatFormulas.RangedHitChance(baseSkill: 50, chebyshevDistance: 1, ammoAccuracyBonus: 0));
    }

    [Fact]
    public void EachCellBeyondTheFirstCostsTwoPointsOfAccuracy() {
        Assert.Equal(42, CombatFormulas.RangedHitChance(baseSkill: 50, chebyshevDistance: 5, ammoAccuracyBonus: 0));
    }

    [Fact]
    public void BetterAmmunitionAddsItsOwnAccuracy() {
        Assert.Equal(60, CombatFormulas.RangedHitChance(baseSkill: 50, chebyshevDistance: 1, ammoAccuracyBonus: 10));
    }

    [Fact]
    public void ARangedChanceNeverGoesNegative() {
        Assert.Equal(0, CombatFormulas.RangedHitChance(baseSkill: 5, chebyshevDistance: 20, ammoAccuracyBonus: 0));
    }

    [Fact]
    public void ARangedShotWithNoChanceAlwaysMissesUnlikeMeleeWhichAlwaysHasATwoPercentFloor() {
        Assert.False(CombatFormulas.RangedHits(roll: 0, hitChance: 0));
    }

    // ---- enchantment ---------------------------------------------------------------------

    [Theory]
    [InlineData(ItemFlags.Poisoned, 10)]     // flat, ignores the weapon
    [InlineData(ItemFlags.Flaming, 15)]      // 20 * 75/100
    [InlineData(ItemFlags.SteelFired, 20)]   // base
    [InlineData(ItemFlags.Frosted, 10)]      // base/2
    [InlineData(ItemFlags.Enhanced1, 40)]    // base*2
    [InlineData(ItemFlags.Enhanced2, 15)]    // base * 75/100
    public void EachEnchantmentAddsItsOwnShareOfTheWeaponBase(ItemFlags flag, int expected) {
        Assert.Equal(expected, CombatFormulas.WeaponEnchantmentBonus(flag, weaponBase: 20));
    }

    [Fact]
    public void EnchantmentsDoNotStackTheLastOneTestedWins() {
        // Poisoned would give 10 and Enhanced1 40; the original's later test overwrites.
        Assert.Equal(40, CombatFormulas.WeaponEnchantmentBonus(
            ItemFlags.Poisoned | ItemFlags.Enhanced1, weaponBase: 20));
    }

    // ---- melee damage --------------------------------------------------------------------

    [Fact]
    public void MeleeDamageIsStrengthPlusTheConditionScaledWeaponBase() {
        // 30 strength + (40 base * 50%) = 50.
        Assert.Equal(50, CombatFormulas.MeleeDamage(
            strength: 30, hasWeapon: true, weaponBase: 40, weaponConditionPercent: 50,
            enchantmentBonus: 0, doubled: false));
    }

    [Fact]
    public void AnUnarmedBlowIsStrengthAlone() {
        Assert.Equal(30, CombatFormulas.MeleeDamage(30, false, 99, 99, 0, false));
    }

    [Fact]
    public void TheGuardaRevancheDoublesEverythingIncludingTheEnchantment() {
        Assert.Equal(100, CombatFormulas.MeleeDamage(
            strength: 30, hasWeapon: true, weaponBase: 40, weaponConditionPercent: 50,
            enchantmentBonus: 0, doubled: true));
    }

    [Fact]
    public void MeleeDamageAlwaysLandsAtLeastOne() {
        Assert.Equal(1, CombatFormulas.MeleeDamage(0, false, 0, 0, enchantmentBonus: -50, doubled: false));
    }

    [Fact]
    public void MeleeDamageIsCappedAt255OnTheCdBuildWeTarget() {
        Assert.Equal(255, CombatFormulas.MeleeDamage(200, true, 200, 100, 200, doubled: true));
    }

    // ---- ranged damage -------------------------------------------------------------------

    [Fact]
    public void ACrossbowShotIsWeaponBasePlusQuarrelBaseAndOwesNothingToStrength() {
        Assert.Equal(25, CombatFormulas.RangedDamage(0, crossbowBase: 15, quarrelBase: 10, Never()));
    }

    [Fact]
    public void AThrownRockRollsItsOwnDamageIgnoringTheWeapon() {
        Assert.Equal(15, CombatFormulas.RangedDamage(CombatFormulas.ThrownRockKind, 99, 99, Rolls(0)));
        Assert.Equal(34, CombatFormulas.RangedDamage(CombatFormulas.ThrownRockKind, 99, 99, Rolls(19)));
    }

    [Fact]
    public void TheOtherFlatKindRollsItsNarrowerBand() {
        Assert.Equal(5, CombatFormulas.RangedDamage(CombatFormulas.FlatRollKind, 99, 99, Rolls(0)));
        Assert.Equal(11, CombatFormulas.RangedDamage(CombatFormulas.FlatRollKind, 99, 99, Rolls(6)));
    }

    [Fact]
    public void AProjectileWithNoQuarrelRecordReportsMinusOne() {
        Assert.Equal(-1, CombatFormulas.RangedDamage(0, crossbowBase: 15, quarrelBase: null, Never()));
    }

    // ---- damage application --------------------------------------------------------------

    private static DamageOutcome Apply(
        int damage, int stamina = 100, int health = 100, bool immune = false, bool applyArmor = false,
        int armorRating = 0, int? absorbPool = null, bool fromDirectAttack = true, bool negated = false,
        bool weak = false, bool resistant = false, Func<int, int> rnd = null) =>
        CombatFormulas.ApplyDamage(damage, stamina, health, immune, applyArmor, armorRating, absorbPool,
                                   fromDirectAttack, negated, weak, resistant, rnd ?? Never());

    [Fact]
    public void AnImmuneTargetTakesNothing() {
        DamageOutcome outcome = Apply(50, immune: true);

        Assert.Equal(0, outcome.DamageDealt);
        Assert.Equal(100, outcome.Stamina);
        Assert.False(outcome.Died);
    }

    [Fact]
    public void DamageBelowOneIsNotAHitAtAll() {
        Assert.Equal(0, Apply(0).DamageDealt);
        Assert.Equal(0, Apply(-5).DamageDealt);
    }

    [Fact]
    public void ArmourRemovesItsPercentageOfTheDamage() {
        Assert.Equal(60, Apply(100, applyArmor: true, armorRating: 40).DamageDealt);
    }

    [Fact]
    public void ArmourCanNeverReduceAHitToNothing() {
        // 1 * (100-90)/100 truncates to 0, so a token 1-2 is rolled instead.
        DamageOutcome outcome = Apply(1, applyArmor: true, armorRating: 90, rnd: Rolls(0));

        Assert.Equal(1, outcome.DamageDealt);
    }

    [Fact]
    public void AnAbsorbShieldSwallowsTheHitEntirelyWhileItHasThePoints() {
        DamageOutcome outcome = Apply(30, absorbPool: 50);

        Assert.Equal(0, outcome.DamageDealt);
        Assert.Equal(20, outcome.AbsorbPool);
        Assert.Equal(100, outcome.Stamina);
        Assert.False(outcome.ShieldBroken);
    }

    [Fact]
    public void OverflowBreaksTheShieldAndOnlyTheRemainderGetsThrough() {
        DamageOutcome outcome = Apply(80, absorbPool: 50);

        Assert.Equal(30, outcome.DamageDealt);
        Assert.Null(outcome.AbsorbPool);
        Assert.True(outcome.ShieldBroken);
        Assert.Equal(70, outcome.Stamina);
    }

    [Fact]
    public void AShieldDoesNotProtectAgainstIndirectDamage() {
        DamageOutcome outcome = Apply(30, absorbPool: 50, fromDirectAttack: false);

        Assert.Equal(30, outcome.DamageDealt);
        Assert.Equal(50, outcome.AbsorbPool);
    }

    [Fact]
    public void NegationZeroesTheDamageOutright() {
        Assert.Equal(0, Apply(80, negated: true).DamageDealt);
    }

    [Fact]
    public void AWeakCreatureTakesHalfAgainAsMuch() {
        Assert.Equal(30, Apply(20, weak: true).DamageDealt);
    }

    [Fact]
    public void AResistantCreatureTakesHalf() {
        Assert.Equal(10, Apply(20, resistant: true).DamageDealt);
    }

    [Fact]
    public void WeaknessAndResistanceAreNotTheSameWayRound() {
        // canassa names these backwards (apply_proficiency_bonus is the x1.5 weakness path and
        // apply_weakness_penalty is the /2 resistance path), so this guards against the swap.
        Assert.True(Apply(20, weak: true).DamageDealt > Apply(20, resistant: true).DamageDealt);
    }

    [Fact]
    public void StaminaAbsorbsTheHitBeforeHealthDoes() {
        DamageOutcome outcome = Apply(40, stamina: 100, health: 100);

        Assert.Equal(60, outcome.Stamina);
        Assert.Equal(100, outcome.Health);
    }

    [Fact]
    public void OnlyTheOverflowPastStaminaReachesHealth() {
        DamageOutcome outcome = Apply(70, stamina: 50, health: 100);

        Assert.Equal(0, outcome.Stamina);
        Assert.Equal(80, outcome.Health);
    }

    [Fact]
    public void TheReportedDamageIsTheFullHitNotJustTheSharpThatReachedHealth() {
        // The original restores the pre-split total for the floating number.
        Assert.Equal(70, Apply(70, stamina: 50, health: 100).DamageDealt);
    }

    [Fact]
    public void HealthRunningOutIsDeathAndNeverGoesNegative() {
        DamageOutcome outcome = Apply(500, stamina: 10, health: 20);

        Assert.Equal(0, outcome.Health);
        Assert.True(outcome.Died);
    }
}
