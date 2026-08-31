namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Character;
using GameData.Resources.Data;
using Xunit;

/// <summary>
/// Springing a chest trap — <c>handle_Container</c> @0x7752b (TASK-265).
/// </summary>
public class ChestTrapSpringTests {
    /// <summary>
    /// THE FENCE. The byte is whole points in a fixed-point amount, so it is shifted left 8 before
    /// being negated — passing it raw deals a 256th of the damage, and nothing on screen says so.
    /// </summary>
    [Theory]
    [InlineData(1, -256)]
    [InlineData(5, -1280)]
    [InlineData(20, -5120)]
    [InlineData(255, -65280)]
    public void TheDamageIsShiftedNotRaw(int trapDamage, long expected) {
        Assert.Equal(expected, ChestTrap.DamageDelta(trapDamage));
        Assert.NotEqual(-trapDamage, ChestTrap.DamageDelta(trapDamage));
    }

    /// <summary>An untrapped chest deals nothing, and the sign convention survives zero.</summary>
    [Fact]
    public void NoTrapIsNoDamage() => Assert.Equal(0, ChestTrap.DamageDelta(0));

    /// <summary>
    /// The scale is the pool's own: a full heal elsewhere is <c>0x7fff</c>, which is 127 whole
    /// points at this shift. Anchoring it here so the two conventions cannot drift apart.
    /// </summary>
    [Fact]
    public void TheScaleMatchesThePoolsOwn() =>
        Assert.Equal(127, 0x7fff >> 8);

    /// <summary>
    /// <b>100 is a heal-target percent, not a <see cref="StatChangeMode"/>.</b> That enum stops at
    /// 3, and the same routine passes a real mode (3, skill use) for its disarm award a few lines
    /// earlier — so the nearer call is the wrong one to copy.
    /// </summary>
    [Fact]
    public void TheThirdArgumentIsNotAStatChangeMode() {
        Assert.False(System.Enum.IsDefined(typeof(StatChangeMode),
            ChestTrap.DamageHealTargetPercent));
        // The mode the disarm award really does use, a few lines earlier in the same routine.
        Assert.Equal(3, (int)StatChangeMode.SkillUse);
    }

    /// <summary>Applying the delta really does drain the combined pool by that many points.</summary>
    [Fact]
    public void TheDeltaDrainsThePoolByWholePoints() {
        var health = new ActorStat { Base = 40, Max = 40 };
        var stamina = new ActorStat { Base = 40, Max = 40 };

        StatEngine.ModifyHealthPool(health, stamina, ChestTrap.DamageDelta(10),
            ChestTrap.DamageHealTargetPercent, out _);

        Assert.Equal(70, StatEngine.HealthPool(health, stamina));
    }
}
