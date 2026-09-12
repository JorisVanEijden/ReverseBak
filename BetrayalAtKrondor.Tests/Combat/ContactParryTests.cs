namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// What a monster in contact with its target does — <c>combataipath_followup_action</c>.
/// </summary>
public class ContactParryTests {
    [Theory]
    [InlineData(0)]
    [InlineData(24)]
    [InlineData(25)]
    public void ROLLSUpToAndIncludingTwentyFiveRaiseAGuard(int roll) {
        // The original is `if (r > 0x19) attack; else parry`, so 25 itself parries — 26 of the 100
        // outcomes do. `>= 25` reads the same and is a point of behaviour out.
        Assert.True(CombatAi.ContactParries(roll));
    }

    [Theory]
    [InlineData(26)]
    [InlineData(50)]
    [InlineData(99)]
    public void ROLLSAboveTwentyFiveAttack(int roll) {
        Assert.False(CombatAi.ContactParries(roll));
    }

    [Fact]
    public void THEBoundIsTheOriginalsHexConstant() {
        Assert.Equal(0x19, CombatAi.ParryRollBound);
    }
}
