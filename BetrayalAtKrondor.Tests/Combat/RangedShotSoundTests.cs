namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The cue a ranged attack plays — <c>resolveRangedAttack</c> @0x66114.
/// </summary>
public class RangedShotSoundTests {
    /// <summary>THE ASYMMETRY. Two cues either side of the projectile flight, not one "shot" sound.
    /// </summary>
    [Fact]
    public void ARockImpactsAndACrossbowDischarges() {
        Assert.Equal(RangedShotSound.RockImpactCue,
            RangedShotSound.Cue(RangedShotSound.ThrownRockKind, attackerHasCrossbow: false));
        Assert.Equal(RangedShotSound.CrossbowFiringCue,
            RangedShotSound.Cue(quarrelKind: 0, attackerHasCrossbow: true));
    }

    /// <summary>
    /// The rock branch is taken on the KIND, before the weapon is consulted — so a rock thrown by
    /// someone carrying a crossbow still impacts and never discharges.
    /// </summary>
    [Fact]
    public void ARockIgnoresTheWeaponEntirely() =>
        Assert.Equal(RangedShotSound.RockImpactCue,
            RangedShotSound.Cue(RangedShotSound.ThrownRockKind, attackerHasCrossbow: true));

    /// <summary>
    /// <b>A thrown weapon with no crossbow equipped is silent.</b> The cue is gated on the attacker
    /// actually holding one; unconditional would give a bowstring twang to a hurled object.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(9)]
    public void WithoutACrossbowANonRockShotIsSilent(int quarrelKind) =>
        Assert.Null(RangedShotSound.Cue(quarrelKind, attackerHasCrossbow: false));

    /// <summary>
    /// The ids are the enum's, not invented: 73 is <c>sound_rockhit</c> and 68 is
    /// <c>sound_crossbow</c>, both in the combat bank the arena preloads.
    /// </summary>
    [Fact]
    public void TheIdsAreTheCombatBanks() {
        Assert.Equal(73, RangedShotSound.RockImpactCue);
        Assert.Equal(68, RangedShotSound.CrossbowFiringCue);
    }
}
