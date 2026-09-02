namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// A flying spell's two cues, and their relationship to the other combat sounds.
/// </summary>
public class SpellProjectileSoundTests {
    /// <summary>Only a cast with a destination actor flies anything.</summary>
    [Fact]
    public void AGroundAimedCastFliesNothing() {
        Assert.False(SpellProjectileSound.Flies(hasTargetActor: false));
        Assert.True(SpellProjectileSound.Flies(hasTargetActor: true));
    }

    /// <summary>The launch and the impact are different sounds.</summary>
    /// <remarks>
    /// Pinned because collapsing them would be inaudible as a bug: a spell would simply make the
    /// same noise twice, once on a miss and twice on a hit.
    /// </remarks>
    [Fact]
    public void TheLaunchIsNotTheImpact() {
        Assert.NotEqual(SpellProjectileSound.LaunchCue, SpellProjectileSound.ImpactCue);
    }

    /// <summary>A magic bolt and a spell burst on the same cue, which is deliberate.</summary>
    /// <remarks>
    /// <c>resolveRangedAttack</c>'s kind-3 arm and <c>Spell_ApplyHitWithProjectile</c> both push
    /// <c>sound_arrowexp</c>. Asserted so that "correcting" one of them to a different id has to be
    /// a deliberate act — they are the same event, a magical projectile bursting on its target.
    /// </remarks>
    [Fact]
    public void AMagicBoltAndASpellShareTheirBurst() {
        Assert.Equal(RangedShotSound.MagicBoltCue, SpellProjectileSound.ImpactCue);
    }

    /// <summary>The spell's launch is not any of the ranged cues.</summary>
    [Fact]
    public void TheSpellLaunchIsItsOwnCue() {
        Assert.NotEqual(RangedShotSound.CrossbowFiringCue, SpellProjectileSound.LaunchCue);
        Assert.NotEqual(RangedShotSound.RockImpactCue, SpellProjectileSound.LaunchCue);
        Assert.NotEqual(RangedShotSound.ImpactCue, SpellProjectileSound.LaunchCue);
    }
}
