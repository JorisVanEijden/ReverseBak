namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary><c>cbstat_armor_coverage_mask</c> — what damage types a swing is made of.</summary>
public class CombatDamageMaskTests {
    [Fact]
    public void APlainSwingStillCarriesTheBaseTypes() =>
        Assert.Equal(0x580, CombatDamageMask.ForSwing(0));

    [Theory]
    [InlineData(0x2000)]
    [InlineData(0x4000)]
    [InlineData(0x8000)]
    public void TheThreeTopBitsAllMeanTheSameType(int flag) =>
        Assert.Equal(0x580 | 0x800, CombatDamageMask.ForSwing(flag));

    [Theory]
    [InlineData(0x0100)]
    [InlineData(0x0200)]
    public void SoDoTheTwoInTheMiddle(int flag) =>
        Assert.Equal(0x580 | 0x004, CombatDamageMask.ForSwing(flag));

    [Fact]
    public void EachRemainingBitAddsItsOwnType() {
        Assert.Equal(0x580 | 0x001, CombatDamageMask.ForSwing(0x0080));
        Assert.Equal(0x580 | 0x002, CombatDamageMask.ForSwing(0x0400));
        Assert.Equal(0x580 | 0x010, CombatDamageMask.ForSwing(0x0800));
        Assert.Equal(0x580 | 0x020, CombatDamageMask.ForSwing(0x1000));
    }

    /// <summary>A blade carrying several coatings carries all of their types.</summary>
    [Fact]
    public void TheBitsAccumulate() =>
        Assert.Equal(0x580 | 0x001 | 0x004 | 0x020,
            CombatDamageMask.ForSwing(0x0080 | 0x0200 | 0x1000));

    /// <summary>
    /// The base value is not inert: eight shipped classes carry one of its bits, so it decides what
    /// a third of the bestiary takes from an ordinary blow.
    /// </summary>
    [Fact]
    public void TheBaseValueOverlapsTheShippedAffinityBits() {
        // 0x080 (classes 22, 26, 56, 57 resist) and 0x100 (30 weak; 48, 49, 61 resist).
        Assert.NotEqual(0, CombatDamageMask.BaseSwing & 0x080);
        Assert.NotEqual(0, CombatDamageMask.BaseSwing & 0x100);
    }
}
