namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The overworld cast dispatcher: nine spells, three of them instantaneous, and everything else
/// silently ignored.
/// </summary>
public class FieldSpellsTests {
    [Fact]
    public void NineSpellsWorkOutsideAFight() {
        Assert.Equal(9, FieldSpells.All.Length);
        foreach (int id in FieldSpells.All) {
            Assert.True(FieldSpells.IsFieldSpell(id));
        }
    }

    [Fact]
    public void NonMartialDoesNotMeanFieldCastable() {
        // Dannon's Delusions and Nightfingers are non-martial combat spells; Eagle Wing, Aether
        // Bridge and Dawn of Truth are non-martial and not dispatched here at all.
        Assert.False(FieldSpells.NonMartialImpliesFieldCastable);
        Assert.False(FieldSpells.IsFieldSpell(SpellIds.DannonsDelusions));
        Assert.False(FieldSpells.IsFieldSpell(SpellIds.Nightfingers));
        Assert.False(FieldSpells.IsFieldSpell(10));
        Assert.False(FieldSpells.IsFieldSpell(24));
        Assert.False(FieldSpells.IsFieldSpell(33));
    }

    [Fact]
    public void AndNoCombatSpellIsInTheList() {
        Assert.False(FieldSpells.IsFieldSpell(SpellIds.Skyfire));
        Assert.False(FieldSpells.IsFieldSpell(SpellIds.MadGodsRage));
        Assert.False(FieldSpells.IsFieldSpell(SpellIds.FinalRest));
    }

    [Fact]
    public void ThreeOfTheNineTakeNoDurationAtAll() {
        Assert.True(FieldSpells.IsInstantaneous(FieldSpells.EyesOfIshap));
        Assert.True(FieldSpells.IsInstantaneous(FieldSpells.TheUnseen));
        Assert.True(FieldSpells.IsInstantaneous(FieldSpells.NacreCicatrix));
    }

    [Fact]
    public void AndTheOtherSixDo() {
        foreach (int id in FieldSpells.All) {
            if (FieldSpells.IsInstantaneous(id)) {
                continue;
            }
            Assert.True(FieldSpells.TakesDuration(id));
        }
    }

    [Fact]
    public void ASpellOutsideTheListTakesNoDurationBecauseItDoesNothing() {
        Assert.False(FieldSpells.TakesDuration(SpellIds.Skyfire));
    }

    [Fact]
    public void StarduskIsAFieldSpellDespiteItsCastingRestrictions() {
        // SpellIds already singles it out for where it may be cast; that is a different question
        // from whether the overworld dispatcher handles it.
        Assert.Equal(SpellIds.Stardusk, FieldSpells.Stardusk);
        Assert.True(FieldSpells.IsFieldSpell(FieldSpells.Stardusk));
    }

    [Fact]
    public void CandleGlowAgreesWithTheIdAlreadyRecorded() {
        Assert.Equal(SpellIds.CandleGlow, FieldSpells.CandleGlow);
    }

    [Fact]
    public void AnUnrecognisedSpellIsSilentlyIgnored() {
        Assert.True(FieldSpells.UnknownSpellDoesNothing);
        Assert.False(FieldSpells.IsFieldSpell(-1));
        Assert.False(FieldSpells.IsFieldSpell(44));
    }
}
