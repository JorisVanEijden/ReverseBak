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

    [Fact]
    public void ALLNINEFieldSpellsHaveTheirCue() {
        // Cast_field_spell's dispatch is a LINEAR SCAN of a nine-entry table, so these nine are the
        // complete field set — there is no tenth to find.
        int[] field = {
            FieldSpells.DragonsBreath, FieldSpells.CandleGlow, FieldSpells.ScentOfSarig,
            FieldSpells.EyesOfIshap, FieldSpells.TheUnseen, FieldSpells.NacreCicatrix,
            FieldSpells.Stardusk, FieldSpells.Union, FieldSpells.AndTheLightShallLie,
        };

        foreach (int spell in field) {
            Assert.True(SpellCastSound.IsEstablished(spell), "field spell " + spell + " unmapped");
            Assert.NotNull(SpellCastSound.ForCast(spell));
        }
    }

    [Fact]
    public void TheINSTANTANEOUSFieldSpellsShareOneCue() {
        // The Unseen and Nacre Cicatrix — two of the three that take no duration — both play
        // sound_spell3, and Eyes of Ishap (the third) plays the Scent cue instead. So the cue does
        // not track the duration/instant split; they are independent properties.
        Assert.Equal(SpellCastSound.ForCast(FieldSpells.TheUnseen),
            SpellCastSound.ForCast(FieldSpells.NacreCicatrix));
        Assert.NotEqual(SpellCastSound.ForCast(FieldSpells.EyesOfIshap),
            SpellCastSound.ForCast(FieldSpells.TheUnseen));
        Assert.True(FieldSpells.IsInstantaneous(FieldSpells.EyesOfIshap));
    }

    [Fact]
    public void TheCueConstantsAreTheONESFieldSpellsAlreadyNames() {
        // Not re-literalled here: FieldSpells already named these, and two spellings of 58 in one
        // codebase is how they drift apart.
        Assert.Equal(FieldSpells.CreationSound, SpellCastSound.ForCast(FieldSpells.DragonsBreath));
        Assert.Equal(FieldSpells.GeneralSound, SpellCastSound.ForCast(FieldSpells.Union));
        Assert.Equal(FieldSpells.ScentSound, SpellCastSound.ForCast(FieldSpells.ScentOfSarig));
    }
}
