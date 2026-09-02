namespace BetrayalAtKrondor.Tests.Combat;

using System.Linq;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The cues a landed shot plays, which are not the cues a fired shot plays.
/// </summary>
/// <remarks>
/// <b>The two halves are easy to conflate and the original keeps them apart.</b>
/// <see cref="RangedShotSound.Cue"/> is the departure — it plays whether or not the shot connects,
/// and is silent for a thrown weapon with no crossbow. <see cref="RangedShotSound.HitCues"/> is the
/// arrival, behind the to-hit roll. A port that plays only the first is silent on every hit; one
/// that puts the first behind the hit test is silent on every miss, which is the common case.
/// </remarks>
public class RangedShotHitCueTests {
    [Fact]
    public void AnOrdinaryHitPlaysTheImpactAlone() {
        Assert.Equal(new[] { RangedShotSound.ImpactCue },
            RangedShotSound.HitCues(quarrelKind: 0).ToArray());
    }

    /// <summary>A magic bolt plays BOTH — the impact, then its own burst.</summary>
    [Fact]
    public void AMagicBoltPlaysTheImpactThenItsBurst() {
        Assert.Equal(new[] { RangedShotSound.ImpactCue, RangedShotSound.MagicBoltCue },
            RangedShotSound.HitCues(RangedShotSound.MagicBoltKind).ToArray());
    }

    /// <summary>Every kind lands with a sound, so no kind is silently mute on impact.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void EveryQuarrelKindMakesAnImpactSound(int kind) {
        Assert.Contains(RangedShotSound.ImpactCue, RangedShotSound.HitCues(kind));
    }

    /// <summary>The departure and the arrival are different sounds, for every kind.</summary>
    /// <remarks>
    /// Pinned because the failure it guards against is invisible: if the two ever collapsed onto one
    /// id, a shot would just sound twice and nothing would report it.
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(RangedShotSound.ThrownRockKind)]
    [InlineData(RangedShotSound.MagicBoltKind)]
    public void TheDepartureIsNeverTheArrival(int kind) {
        int? departure = RangedShotSound.Cue(kind, attackerHasCrossbow: true);
        Assert.NotNull(departure);
        Assert.DoesNotContain(departure!.Value, RangedShotSound.HitCues(kind));
    }
}
