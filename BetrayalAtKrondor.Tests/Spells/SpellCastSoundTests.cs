namespace BetrayalAtKrondor.Tests.Spells;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// Which sound a spell makes when it goes off.
/// </summary>
public class SpellCastSoundTests {
    [Fact]
    public void SEVERALSpellsShareOneCue() {
        // The cue belongs to the KIND of magic, not to the spell: three different summoning-ish
        // spells all play sound_mcreate. A one-sound-per-spell table would invent nine cues the
        // game does not have.
        Assert.Equal(58, SpellCastSound.ForCast(SpellIds.CandleGlow));
        Assert.Equal(58, SpellCastSound.ForCast(SpellIds.Stardusk));
        Assert.Equal(58, SpellCastSound.ForCast(SpellIds.Steelfire));
        Assert.Equal(81, SpellCastSound.ForCast(SpellIds.Nightfingers));
        Assert.Equal(81, SpellCastSound.ForCast(SpellIds.Invitation));
    }

    [Fact]
    public void ASpellWithNoMappingReportsNothing() {
        Assert.Null(SpellCastSound.ForCast(SpellIds.Skyfire));
        Assert.False(SpellCastSound.IsEstablished(SpellIds.Skyfire));
    }

    [Fact]
    public void SILENTISNOTTHESAMEASUNMAPPED() {
        // *** The distinction worth keeping. *** Evil Seek pushes no sound at all — verified
        // absence. Skyfire simply has not been looked at. Both return null from ForCast, and
        // collapsing them would make an unmapped spell look deliberately silent so nobody ever
        // comes back to it.
        Assert.Null(SpellCastSound.ForCast(SpellIds.EvilSeek));
        Assert.True(SpellCastSound.IsEstablished(SpellIds.EvilSeek));
        Assert.True(SpellCastSound.IsSilent(SpellIds.EvilSeek));

        Assert.False(SpellCastSound.IsSilent(SpellIds.Skyfire));
        Assert.False(SpellCastSound.IsEstablished(SpellIds.Skyfire));
    }

    [Fact]
    public void ASpellWithACueIsEstablishedButNotSilent() {
        Assert.True(SpellCastSound.IsEstablished(SpellIds.Flamecast));
        Assert.False(SpellCastSound.IsSilent(SpellIds.Flamecast));
    }

    [Fact]
    public void MADGODSRAGEPlaysASecondCuePerTarget() {
        // Two sounds at two moments. A port that gives a spell one cue plays the quake and drops the
        // rest, so a rage that hits five enemies sounds like one that hits nobody.
        Assert.Equal(78, SpellCastSound.ForCast(SpellIds.MadGodsRage));
        Assert.Equal(29, SpellCastSound.PerTarget(SpellIds.MadGodsRage));
        Assert.NotEqual(SpellCastSound.ForCast(SpellIds.MadGodsRage),
            SpellCastSound.PerTarget(SpellIds.MadGodsRage));
    }

    [Fact]
    public void OtherSpellsHaveNoPerTargetCue() {
        Assert.Null(SpellCastSound.PerTarget(SpellIds.Flamecast));
        Assert.Null(SpellCastSound.PerTarget(SpellIds.EvilSeek));
    }

    [Fact]
    public void TheTwoArrowSpellsShareTheArrowCue() {
        Assert.Equal(1, SpellCastSound.ForCast(SpellIds.Flamecast));
        Assert.Equal(1, SpellCastSound.ForCast(SpellIds.BlackNimbus));
    }
}
