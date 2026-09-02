namespace BetrayalAtKrondor.Tests.Spells;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The two spells <c>Cast_Spell</c>'s switch gives <c>sound_sparkly</c>.
/// </summary>
/// <remarks>
/// Decoded from the pushes at 0x6887e (case 20, Unfortunate Flux) and 0x688a8 (case 13, Grief of
/// 1000 Nights). Grief was already in the table; Flux was the entry that was missing, and a cue
/// table with a hole in it reads as "that spell is silent" rather than as "nobody looked" — which is
/// the distinction <see cref="SpellCastSound.IsEstablished"/> exists to preserve.
/// </remarks>
public class SparklyCueTests {
    private const int Sparkly = 77;

    [Fact]
    public void BothSparklySpellsAreMapped() {
        Assert.Equal(Sparkly, SpellCastSound.ForCast(SpellIds.GriefOfAThousandNights));
        Assert.Equal(Sparkly, SpellCastSound.ForCast(SpellIds.UnfortunateFlux));
    }

    /// <summary>Neither is recorded as silent, which would be the opposite claim.</summary>
    [Fact]
    public void NeitherIsSilent() {
        Assert.False(SpellCastSound.IsSilent(SpellIds.GriefOfAThousandNights));
        Assert.False(SpellCastSound.IsSilent(SpellIds.UnfortunateFlux));
        Assert.True(SpellCastSound.IsEstablished(SpellIds.UnfortunateFlux));
    }
}
