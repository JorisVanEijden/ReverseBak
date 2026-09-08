namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The absorb shield and the negation, and the two ways wiring them goes wrong — TASK-377.
/// </summary>
/// <remarks>
/// <c>CombatFormulas.ApplyDamage</c> implemented both rules from the start; every production caller
/// passed <c>null</c> and <c>false</c>, so Hocho's Haven and Skin of the Dragon protected nobody.
/// These pin the seam rather than the arithmetic.
/// </remarks>
public class AbsorbShieldWiringTests {
    private static int NoRoll(int n) => 0;

    [Fact]
    public void AShieldBiggerThanTheHitTakesALLOfIt_andReportsWhatIsLeft() {
        DamageOutcome outcome = CombatFormulas.ApplyDamage(
            damage: 12, stamina: 30, health: 40, immune: false,
            applyArmor: false, armorRating: 0,
            absorbPool: 40, fromDirectAttack: true, negated: false,
            weakToDamageType: false, resistsDamageType: false, rnd: NoRoll);

        Assert.Equal(30, outcome.Stamina);
        Assert.Equal(40, outcome.Health);
        Assert.Equal(28, outcome.AbsorbPool);
    }

    [Fact]
    public void ASHIELDTHATBREAKSLetsTheOverflowThroughAndReportsNull() {
        // The overflow is the part the shield could not cover, not the whole blow — 12 against 5
        // leaves 7. A port that dropped the whole hit would make a nearly-spent shield as good as a
        // full one.
        DamageOutcome outcome = CombatFormulas.ApplyDamage(
            damage: 12, stamina: 30, health: 40, immune: false,
            applyArmor: false, armorRating: 0,
            absorbPool: 5, fromDirectAttack: true, negated: false,
            weakToDamageType: false, resistsDamageType: false, rnd: NoRoll);

        Assert.Null(outcome.AbsorbPool);
        Assert.Equal(30 - 7, outcome.Stamina);
    }

    [Fact]
    public void THESHIELDDOESNOTAPPLYToIndirectDamage() {
        // The original gates both rules on source_type == 0. Spell damage and the cast cost are not
        // direct attacks, so wiring every site uniformly would give the shield to damage the
        // original never protects against.
        DamageOutcome outcome = CombatFormulas.ApplyDamage(
            damage: 12, stamina: 30, health: 40, immune: false,
            applyArmor: false, armorRating: 0,
            absorbPool: 40, fromDirectAttack: false, negated: false,
            weakToDamageType: false, resistsDamageType: false, rnd: NoRoll);

        Assert.Equal(40, outcome.AbsorbPool);
        Assert.Equal(18, outcome.Stamina);
    }

    [Fact]
    public void NegationZeroesADirectHitAndLeavesTheTargetUntouched() {
        DamageOutcome outcome = CombatFormulas.ApplyDamage(
            damage: 25, stamina: 30, health: 40, immune: false,
            applyArmor: false, armorRating: 0,
            absorbPool: null, fromDirectAttack: true, negated: true,
            weakToDamageType: false, resistsDamageType: false, rnd: NoRoll);

        Assert.Equal(30, outcome.Stamina);
        Assert.Equal(40, outcome.Health);
    }

    [Fact]
    public void AMISSCarriesNoPool_whichIsWhyTheWriteBackIsGuarded() {
        // Result.Miss reports null because no damage was applied. Committing that null would DELETE
        // a shield the swing never touched, so CombatRuntime writes back only on a hit. This pins
        // the property that makes the guard necessary.
        Assert.False(MeleeExchange.Result.Miss.Hit);
        Assert.Null(MeleeExchange.Result.Miss.AbsorbPool);
    }

    [Fact]
    public void ANUNPOISONEDTickIsTOLDApartFromOneFullyAbsorbed() {
        // Both report zero damage and a null-or-reduced pool. Without Ticked a caller cannot tell
        // "nothing happened" from "the shield soaked it", and committing on the first deletes a
        // shield that was never hit.
        Assert.False(PoisonTick.Result.None.Ticked);
        Assert.True(new PoisonTick.Result(0, 28, died: false).Ticked);
    }
}
