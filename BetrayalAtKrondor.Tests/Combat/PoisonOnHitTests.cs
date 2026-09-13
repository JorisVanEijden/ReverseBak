namespace BetrayalAtKrondor.Tests.Combat;

using GameData;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// <c>cbstat_apply_drain_tick</c> — the poisoning a poisoned blade leaves behind, which is the point
/// of the weapon and which nothing in the port used to do.
/// </summary>
public class PoisonOnHitTests {
    private const int PoisonedSwing = CombatDamageMask.BaseSwing | PoisonOnHit.PoisonBit;

    [Fact]
    public void APoisonedBlowPoisons() =>
        Assert.True(PoisonOnHit.Applies(7, PoisonedSwing, 0, default));

    [Fact]
    public void APlainBlowDoesNot() =>
        Assert.False(PoisonOnHit.Applies(7, CombatDamageMask.BaseSwing, 0, default));

    [Fact]
    public void ABlowThatDealtNothingDoesNot() =>
        Assert.False(PoisonOnHit.Applies(0, PoisonedSwing, 0, default));

    [Fact]
    public void APoisonResistantCreatureIsNotPoisoned() =>
        Assert.False(PoisonOnHit.Applies(7, PoisonedSwing, PoisonOnHit.PoisonBit, default));

    // The gate is the original's own: it pushes a damage of 1 and a type of 0x80 through
    // cbstat_damage_apply_protection and asks whether anything survived.
    [Fact]
    public void PoisonProofArmourStopsIt() =>
        Assert.False(PoisonOnHit.Applies(7, PoisonedSwing, 0, ItemFlags.Poisoned));

    [Fact]
    public void OtherArmourEnchantmentsDoNot() =>
        Assert.True(PoisonOnHit.Applies(7, PoisonedSwing, 0, ItemFlags.Frosted));

    [Fact]
    public void TheRankIsTenToFiftyNineInclusive() {
        Assert.Equal(10, PoisonOnHit.Rank(_ => 0));
        Assert.Equal(59, PoisonOnHit.Rank(n => n - 1));
    }
}
