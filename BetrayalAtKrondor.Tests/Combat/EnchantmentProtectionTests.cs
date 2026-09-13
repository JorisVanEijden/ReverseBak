namespace BetrayalAtKrondor.Tests.Combat;

using GameData;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// <c>cbstat_damage_apply_protection</c> (CBSTAT.C:284) — armour that matches the blow's element
/// cancels the enchantment bonus outright, and the original's own off-by-one is kept.
/// </summary>
public class EnchantmentProtectionTests {
    [Fact]
    public void PlainArmourTakesTheWholeBonus() =>
        Assert.Equal(12, CombatFormulas.EnchantmentAfterProtection(
            12, (int)ItemFlags.Frosted, default(ItemFlags)));

    [Fact]
    public void MatchingArmourCancelsItOutright() =>
        Assert.Equal(0, CombatFormulas.EnchantmentAfterProtection(
            12, (int)ItemFlags.Frosted, ItemFlags.Frosted));

    [Fact]
    public void ProtectionIsAllOrNothingNotAPercentage() {
        Assert.Equal(0, CombatFormulas.EnchantmentAfterProtection(
            255, (int)ItemFlags.Poisoned, ItemFlags.Poisoned));
        Assert.Equal(255, CombatFormulas.EnchantmentAfterProtection(
            255, (int)ItemFlags.Poisoned, ItemFlags.Frosted));
    }

    // *** THE ORIGINAL'S BUG, KEPT ON PURPOSE. *** Its table tests the FLAMING armour flag against
    // STEEL-FIRED damage, so nothing in the game protects against a flaming weapon and steel-fired
    // damage is stopped by either flag. "Fixing" it would give fire armour a defence the original
    // never grants.
    [Fact]
    public void NothingProtectsAgainstAFlamingWeapon() {
        Assert.Equal(9, CombatFormulas.EnchantmentAfterProtection(
            9, (int)ItemFlags.Flaming, ItemFlags.Flaming));
        Assert.Equal(9, CombatFormulas.EnchantmentAfterProtection(
            9, (int)ItemFlags.Flaming, ItemFlags.SteelFired));
    }

    [Fact]
    public void FlamingArmourStopsSteelFiredDamage() =>
        Assert.Equal(0, CombatFormulas.EnchantmentAfterProtection(
            9, (int)ItemFlags.SteelFired, ItemFlags.Flaming));

    [Fact]
    public void TheDamageTypeIsTheHighestEnchantmentBitPresent() {
        Assert.Equal((int)ItemFlags.Frosted,
            CombatFormulas.EnchantmentDamageType(ItemFlags.Poisoned | ItemFlags.Frosted));
        Assert.Equal(0, CombatFormulas.EnchantmentDamageType(default(ItemFlags)));
    }
}
