namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;

using Xunit;

public class SpellCastRoutinesFlamecastTests {
    [Fact]
    public void TheSplashIsAQuarterOfTheMagnitudeLessOnePerCell() {
        // Measured live: a 60-point Flamecast splashed actors two cells from its target for 13.
        Assert.Equal(13, SpellCastRoutines.FlamecastSplashDamage(60, 2));
        Assert.Equal(14, SpellCastRoutines.FlamecastSplashDamage(60, 1));
        Assert.Equal(2, SpellCastRoutines.FlamecastSplashRadius);
    }
}
