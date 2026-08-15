namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The shared locator screen, and the world-loop cast button.
/// </summary>
public class LocatorScreenTests {
    [Fact]
    public void TheThreeLocatorsDifferOnlyInWhatTheySearchFor() {
        Assert.Equal(FieldSpells.LocatorTarget.Valuables,
            FieldSpells.TargetOf(FieldSpells.EyesOfIshap));
        Assert.Equal(FieldSpells.LocatorTarget.Food, FieldSpells.TargetOf(FieldSpells.TheUnseen));
        Assert.Equal(FieldSpells.LocatorTarget.Magic,
            FieldSpells.TargetOf(FieldSpells.NacreCicatrix));
    }

    [Fact]
    public void AndNothingElseHasATarget() {
        Assert.Equal(FieldSpells.LocatorTarget.None,
            FieldSpells.TargetOf(FieldSpells.DragonsBreath));
        Assert.Equal(FieldSpells.LocatorTarget.None, FieldSpells.TargetOf(SpellIds.Skyfire));
    }

    [Fact]
    public void EveryLocatorSpellHasADistinctTarget() {
        var seen = new System.Collections.Generic.HashSet<FieldSpells.LocatorTarget>();
        foreach (int id in FieldSpells.All) {
            if (!FieldSpells.IsLocatorRoll(id)) {
                continue;
            }
            Assert.True(seen.Add(FieldSpells.TargetOf(id)));
        }
        Assert.Equal(3, seen.Count);
    }

    [Fact]
    public void TheScreenBorrowsTheWorldViewRatherThanBeingAMapScreen() {
        Assert.True(FieldSpells.LocatorReusesTheWorldViewport);
        Assert.Equal((134, 16, 167, 89), FieldSpells.LocatorViewport);
    }

    [Fact]
    public void TheCastButtonNeedsOnlyOneCasterInTheParty() {
        Assert.True(SpellCasting.CastButtonIsUsable(new[] { 0, 0, 40 }));
        Assert.False(SpellCasting.CastButtonIsUsable(new[] { 0, 0, 0 }));
    }

    [Fact]
    public void AndItsStoredFlagIsInverted() {
        // Zero means usable; a port reading it the natural way round greys out exactly the parties
        // that should have the button.
        Assert.Equal(0, SpellCasting.CastButtonFlag(usable: true));
        Assert.Equal(1, SpellCasting.CastButtonFlag(usable: false));
    }

    [Fact]
    public void AnAbsentPartyIsNotACaster() {
        Assert.False(SpellCasting.CastButtonIsUsable(null));
    }
}
