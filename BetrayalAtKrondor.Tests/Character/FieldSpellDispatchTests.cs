namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// Which running-effect slot and which text a field spell owns — <c>Cast_field_spell</c>'s six
/// timed handlers.
/// </summary>
public class FieldSpellDispatchTests {
    [Fact]
    public void TheEffectSlotIsTheDISPATCHOrderAndNotTheSpellNumber() {
        // The two orders disagree from the third entry on. Indexing the mask by spell number would
        // put Stardusk in slot 26 — outside the nine-bit mask entirely — and show the wrong symbol
        // in the travel strip for the ones that do land inside it.
        Assert.Equal(0, FieldSpells.EventIdOf(FieldSpells.DragonsBreath));
        Assert.Equal(1, FieldSpells.EventIdOf(FieldSpells.CandleGlow));
        Assert.Equal(2, FieldSpells.EventIdOf(FieldSpells.Stardusk));
        Assert.Equal(3, FieldSpells.EventIdOf(FieldSpells.AndTheLightShallLie));
        Assert.Equal(4, FieldSpells.EventIdOf(FieldSpells.Union));
        Assert.Equal(5, FieldSpells.EventIdOf(FieldSpells.ScentOfSarig));
        Assert.NotEqual(FieldSpells.Stardusk, FieldSpells.EventIdOf(FieldSpells.Stardusk));
    }

    [Fact]
    public void EverySlotFitsTheMaskAndNoTwoSpellsShareOne() {
        var seen = new System.Collections.Generic.HashSet<int>();
        foreach (int spell in FieldSpells.All) {
            int slot = FieldSpells.EventIdOf(spell);
            if (slot < 0) {
                continue;
            }
            Assert.InRange(slot, 0, SpellPaletteEvents.Count - 1);
            Assert.True(seen.Add(slot), $"spell {spell} reuses slot {slot}");
        }
        Assert.Equal(6, seen.Count);
    }

    [Fact]
    public void ALocatorOwnsNoSlotAndNoDialog() {
        // They finish as they are cast, so there is nothing for the strip to show and no timed text.
        foreach (int spell in new[] {
            FieldSpells.EyesOfIshap, FieldSpells.TheUnseen, FieldSpells.NacreCicatrix }) {
            Assert.True(FieldSpells.IsLocatorRoll(spell));
            Assert.Equal(-1, FieldSpells.EventIdOf(spell));
            Assert.Equal(-1, FieldSpells.DialogFor(spell));
        }
    }

    [Fact]
    public void TheSixDialogsAreConsecutiveFromDragonsBreath() {
        Assert.Equal(199, FieldSpells.DialogFor(FieldSpells.DragonsBreath));
        Assert.Equal(200, FieldSpells.DialogFor(FieldSpells.CandleGlow));
        Assert.Equal(202, FieldSpells.DialogFor(FieldSpells.AndTheLightShallLie));
        Assert.Equal(204, FieldSpells.DialogFor(FieldSpells.ScentOfSarig));
    }

    [Fact]
    public void TheSoundGroupingCutsACROSSTheDurationGrouping() {
        // Three lighting spells share one sound and also share the power-extends-it formula; the
        // other three split two-and-one on sound while sharing one formula. So neither grouping
        // can be derived from the other.
        Assert.Equal(FieldSpells.CreationSound, FieldSpells.SoundFor(FieldSpells.DragonsBreath));
        Assert.Equal(FieldSpells.CreationSound, FieldSpells.SoundFor(FieldSpells.Stardusk));
        Assert.Equal(FieldSpells.GeneralSound, FieldSpells.SoundFor(FieldSpells.Union));
        Assert.Equal(FieldSpells.GeneralSound, FieldSpells.SoundFor(FieldSpells.AndTheLightShallLie));
        Assert.Equal(FieldSpells.ScentSound, FieldSpells.SoundFor(FieldSpells.ScentOfSarig));
        Assert.NotEqual(FieldSpells.SoundFor(FieldSpells.Union),
            FieldSpells.SoundFor(FieldSpells.ScentOfSarig));
        Assert.False(FieldSpells.PowerExtendsDuration(FieldSpells.Union));
        Assert.False(FieldSpells.PowerExtendsDuration(FieldSpells.ScentOfSarig));
    }

    [Fact]
    public void OnlyTheLightingThreeGetLongerWithPower() {
        // duration 2, cost 10: the lighting formula gives 600 ticks and the plain one 60.
        Assert.Equal(600, FieldSpells.DurationTicks(2, 10,
            FieldSpells.PowerExtendsDuration(FieldSpells.DragonsBreath)));
        Assert.Equal(60, FieldSpells.DurationTicks(2, 10,
            FieldSpells.PowerExtendsDuration(FieldSpells.ScentOfSarig)));
    }
}
