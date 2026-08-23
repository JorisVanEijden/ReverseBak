namespace BetrayalAtKrondor.Tests.Spells;

using GameData.Resources.Spells;
using Xunit;

/// <summary>Magnitude adjustment and hit determination for a cast.</summary>
public class SpellCastResolutionTests {
    [Fact]
    public void ANegativeMagnitudeIsAHeal() {
        // The sign is the ONLY thing separating damage from healing, so clamping intensity at zero
        // would silently turn every heal into a no-op.
        Assert.True(SpellCastResolution.IsHeal(-10));
        Assert.False(SpellCastResolution.IsHeal(10));
    }

    [Fact]
    public void TheStormAmplifiesHealsAsWellAsDamage() {
        Assert.Equal(15, SpellCastResolution.ApplyStormAmplification(10, stormActive: true));
        Assert.Equal(10, SpellCastResolution.ApplyStormAmplification(10, stormActive: false));

        // Applied BEFORE the sign is taken, so heals grow too.
        (int magnitude, bool heals) = SpellCastResolution.Resolve(-10, stormActive: true,
            targetIsVulnerable: false);
        Assert.True(heals);
        Assert.Equal(15, magnitude);
    }

    [Fact]
    public void TheShiftRoundsTowardNegativeInfinity() {
        // -5 + (-5 >> 1) = -5 + -3 = -8, not -7. Reproduced with a shift rather than a division.
        Assert.Equal(-8, SpellCastResolution.ApplyStormAmplification(-5, stormActive: true));
    }

    [Fact]
    public void AVulnerableTargetTakesDouble_IncludingFromAHeal() {
        // The bitmap check is consulted for ANY target, not only hostile ones.
        Assert.Equal(20, SpellCastResolution.ApplyVulnerability(10, targetIsVulnerable: true));

        (int magnitude, bool heals) = SpellCastResolution.Resolve(-10, stormActive: false,
            targetIsVulnerable: true);
        Assert.True(heals);
        Assert.Equal(20, magnitude);
    }

    [Fact]
    public void MOSTCastsCannotMiss() {
        // *** The branch a port gets wrong. *** Only an offensive kind-0 spell with a target rolls.
        // Rolling for everything would have healers failing to heal.
        Assert.True(SpellCastResolution.NeedsSkillCheck(0, isHeal: false, hasTarget: true));
        Assert.False(SpellCastResolution.NeedsSkillCheck(0, isHeal: true, hasTarget: true));
        Assert.False(SpellCastResolution.NeedsSkillCheck(3, isHeal: false, hasTarget: true));
        Assert.False(SpellCastResolution.NeedsSkillCheck(0, isHeal: false, hasTarget: false));
    }

    [Fact]
    public void KindEightDiscardsItsTarget() {
        Assert.Equal(8, SpellCastResolution.TargetlessSpellKind);
        // With the target discarded, the skill check cannot apply either.
        Assert.False(SpellCastResolution.NeedsSkillCheck(
            SpellCastResolution.TargetlessSpellKind, isHeal: false, hasTarget: false));
    }

    [Fact]
    public void StormThenSignThenVulnerability() {
        // Order matters: doubling before the storm would compound differently.
        // 10 -> storm 15 -> vulnerable 30.
        (int magnitude, bool heals) = SpellCastResolution.Resolve(10, true, true);
        Assert.False(heals);
        Assert.Equal(30, magnitude);
    }
}
