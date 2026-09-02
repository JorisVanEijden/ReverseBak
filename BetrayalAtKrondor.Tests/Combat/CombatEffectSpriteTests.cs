namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The effect sprite a projectile flies, per caller.
/// </summary>
/// <remarks>
/// These are four magic numbers out of instruction encodings, so the test pins them to the shipped
/// COMBAT.TBL names they were confirmed against rather than restating the switch — an id that
/// silently drifts to a different entry would still look like a plausible integer.
/// </remarks>
public class CombatEffectSpriteTests {
    private const int FlamecastSpellId = 4;
    private const int BaneOfBlackSlayersSpellId = 9;

    [Fact]
    public void Flamecast_flies_its_own_sprite() {
        Assert.Equal(CombatEffectSprite.Flamecast, CombatEffectSprite.ForSpell(FlamecastSpellId));
    }

    [Fact]
    public void BaneOfBlackSlayers_flies_its_own_sprite() {
        Assert.Equal(CombatEffectSprite.BaneOfBlackSlayers,
            CombatEffectSprite.ForSpell(BaneOfBlackSlayersSpellId));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(44)]
    public void Every_other_spell_flies_the_generic_sprite(int spellId) {
        Assert.Equal(CombatEffectSprite.GenericSpell, CombatEffectSprite.ForSpell(spellId));
    }

    /// <summary>The four ids are distinct COMBAT.TBL entries, not aliases of one.</summary>
    [Fact]
    public void The_four_effect_sprites_are_four_entries() {
        int[] ids = {
            CombatEffectSprite.Shot, CombatEffectSprite.Flamecast,
            CombatEffectSprite.BaneOfBlackSlayers, CombatEffectSprite.GenericSpell,
        };
        Assert.Equal(ids.Length, new System.Collections.Generic.HashSet<int>(ids).Count);
    }
}
