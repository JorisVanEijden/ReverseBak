namespace BetrayalAtKrondor.Tests.Spells;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// Which path a spell's cast cue belongs to — field or combat.
/// </summary>
/// <remarks>
/// <b>The split came from a caller sweep, not from what the spells look like.</b> Every routine
/// behind the field set is reached only by <c>Cast_field_spell</c>; every routine behind the combat
/// set only by <c>Cast_Spell</c>, whose callers are all arena routines and monster casters. Eleven
/// entries were on the field side until 2026-09-02, so they sounded where the original is silent.
/// These tests pin the sides so a future entry has to choose one deliberately.
/// </remarks>
public class CastCuePathTests {
    [Theory]
    [InlineData(SpellIds.CandleGlow)]
    [InlineData(SpellIds.Stardusk)]
    public void FieldSpellsSoundOnTheFieldOnly(int spellId) {
        Assert.NotNull(SpellCastSound.ForCast(spellId));
        Assert.Null(SpellCastSound.ForCombatSpell(spellId));
    }

    [Theory]
    [InlineData(SpellIds.BlackNimbus)]
    [InlineData(SpellIds.Steelfire)]
    [InlineData(SpellIds.StrengthDrain)]
    [InlineData(SpellIds.MadGodsRage)]
    [InlineData(SpellIds.WindsOfEortis)]
    [InlineData(SpellIds.Nightfingers)]
    [InlineData(SpellIds.Invitation)]
    [InlineData(SpellIds.DespairThyEyes)]
    [InlineData(SpellIds.UnfortunateFlux)]
    [InlineData(SpellIds.SkinOfTheDragon)]
    public void CombatSpellsSoundInCombatOnly(int spellId) {
        Assert.NotNull(SpellCastSound.ForCombatSpell(spellId));
        Assert.Null(SpellCastSound.ForCast(spellId));
    }

    /// <summary>Grief is the one guarded cue: no susceptible target, no sound.</summary>
    [Fact]
    public void GriefSoundsOnlyAgainstASusceptibleTarget() {
        Assert.NotNull(SpellCastSound.ForCombatSpell(
            SpellIds.GriefOfAThousandNights, targetIsSusceptible: true));
        Assert.Null(SpellCastSound.ForCombatSpell(
            SpellIds.GriefOfAThousandNights, targetIsSusceptible: false));
    }

    /// <summary>Flamecast belongs to neither table — its routine is the cannon ray's.</summary>
    /// <remarks>
    /// It had a field entry whose VALUE was right by coincidence: a player's Flamecast does play cue
    /// 1, but through the projectile path, which is ported separately. Keeping it would have doubled
    /// the sound.
    /// </remarks>
    [Fact]
    public void FlamecastIsInNeitherCastTable() {
        Assert.Null(SpellCastSound.ForCast(SpellIds.Flamecast));
        Assert.Null(SpellCastSound.ForCombatSpell(SpellIds.Flamecast));
    }
}
