namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The +10 field's polymorphism (<c>nEffect_sprite_id</c>). The shipped values are the point: a
/// summoning spell's 44 means <i>spider</i>, not a colour, and only the spell's kind says which.
/// </summary>
public class SpellEffectSubjectTests {
    private static Spell Of(int kind, int subject) =>
        new Spell("s") { TargetingType = kind, EffectSubject = subject };

    [Theory]
    [InlineData(44)] // Arachnos     -> spider
    [InlineData(56)] // River Song   -> Rusalki
    [InlineData(57)] // Riftmare     -> Shade
    public void ASummoningSpellNamesTheCreatureItSummons(int creatureId) {
        Spell spell = Of(kind: 6, subject: creatureId);

        Assert.Equal(creatureId, spell.SummonedCreatureId);
        Assert.Null(spell.TileEffectId);
    }

    [Fact]
    public void ATileSpellNamesItsGridEffect() {
        Spell mirrorwall = Of(kind: 5, subject: 7);

        Assert.Equal(7, mirrorwall.TileEffectId);
        Assert.Null(mirrorwall.SummonedCreatureId);
    }

    [Fact]
    public void AKindFiveSpellWithNoTileEffectReportsNone() {
        // Asphyxiation ships as kind 5 with -1.
        Assert.Null(Of(kind: 5, subject: -1).TileEffectId);
    }

    [Fact]
    public void AVisualSpellsValueIsNeitherACreatureNorATile() {
        // Kind 1 and friends use the field as a sprite / colour index, so both typed readings
        // must stay empty — this is the misreading the rename exists to prevent.
        Spell flamecast = Of(kind: 1, subject: 44);

        Assert.Null(flamecast.SummonedCreatureId);
        Assert.Null(flamecast.TileEffectId);
        Assert.Equal(44, flamecast.EffectSubject);
    }

    [Fact]
    public void ASummonWithNoCreatureReportsNone() {
        Assert.Null(Of(kind: 6, subject: -1).SummonedCreatureId);
    }
}
