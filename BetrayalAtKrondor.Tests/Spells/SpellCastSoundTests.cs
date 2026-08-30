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

    [Fact]
    public void SPELLSWITHNODEDICATEDROUTINEStillHaveTheirCue() {
        // Recovered from Cast_Spell's switch rather than from a Cast_* function. These three were
        // GUESSES in docs/Sound that the per-spell pass could not confirm — "grief of 1000 nights",
        // "despair thy eyes", "skin of the dragon" — and the switch's case values confirm all three.
        Assert.Equal(77, SpellCastSound.ForCast(SpellIds.GriefOfAThousandNights));
        Assert.Equal(FieldSpells.CreationSound, SpellCastSound.ForCast(SpellIds.DespairThyEyes));
        Assert.Equal(FieldSpells.CreationSound, SpellCastSound.ForCast(SpellIds.SkinOfTheDragon));
    }

    [Fact]
    public void ASpellCanShareACueAcrossBothDispatchPaths() {
        // Skin of the Dragon comes from the switch and Steelfire from its own routine, and they play
        // the same cue — so "which dispatcher casts it" is independent of "what it sounds like".
        Assert.Equal(SpellCastSound.ForCast(SpellIds.Steelfire),
            SpellCastSound.ForCast(SpellIds.SkinOfTheDragon));
    }
    [Fact]
    public void AHEALMakesNoCastNoiseAndNoAnimation() {
        // *** THE WHOLE KIND SWITCH SITS INSIDE `if (isNeg == 0)`. *** A negated cost skips the cue
        // AND the wind-up or swing together. A port that plays the cue first and animates second
        // gets a healer who swings at their own side.
        Assert.Null(SpellCastSound.ForCombatCast(0, costWasNegated: true));
        Assert.Null(SpellCastSound.ForCombatCast(1, costWasNegated: true));
        Assert.NotNull(SpellCastSound.ForCombatCast(0, costWasNegated: false));
    }

    [Fact]
    public void THEDEFAULTARMISMELEE_NotSilence() {
        // Kinds 1 and 4 share the default arm, so an UNRECOGNISED kind swings. Only 5 and 6 are
        // quiet, and they are quiet by having cases of their own — carved OUT of the default.
        // Reading it as "0/2/3/7/8 ranged, 1/4 melee, rest silent" inverts every kind above 8.
        foreach (int ranged in new[] { 0, 2, 3, 7, 8 }) {
            Assert.Equal(SpellCastSound.RangedWindupCue,
                SpellCastSound.ForCombatCast(ranged, costWasNegated: false));
        }

        foreach (int melee in new[] { 1, 4, 9, 42 }) {
            Assert.Equal(SpellCastSound.MeleeSwingCue,
                SpellCastSound.ForCombatCast(melee, costWasNegated: false));
        }

        Assert.Null(SpellCastSound.ForCombatCast(5, costWasNegated: false));
        Assert.Null(SpellCastSound.ForCombatCast(6, costWasNegated: false));
    }

    [Fact]
    public void TheCombatCuesAreADIFFERENTTableFromTheFieldOnes() {
        // ForCast keys on the SPELL and is what the field caster plays; ForCombatCast keys on the
        // KIND and is what a fight plays. Neither is a fallback for the other, and the same spell
        // cast in the field and in combat does not make the same noise.
        Assert.Equal(0x12, SpellCastSound.RangedWindupCue);
        Assert.Equal(0x13, SpellCastSound.MeleeSwingCue);
        Assert.Equal(0x3b, SpellCastSound.ResistedCue);
        Assert.Equal(0x3f, SpellCastSound.PoolDeliveryCue);
    }

}
