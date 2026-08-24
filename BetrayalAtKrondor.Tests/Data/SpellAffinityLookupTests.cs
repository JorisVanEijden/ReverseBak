namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Spells;

using Xunit;

/// <summary>
/// The membership test every caller of SPELLWEA/SPELLRES makes —
/// <c>check_spell_weakness</c> / <c>check_spell_resistance</c> (ovr177 @0x6b5db, @0x6b595).
/// </summary>
public class SpellAffinityLookupTests {
    private static SpellAffinityTable Table() {
        var t = new SpellAffinityTable("SPELLWEA.DAT");
        t.Spells.Add(new SpellAffinity { SpellNumber = 0, CreatureTypes = { 0, 5, 47 } });
        t.Spells.Add(new SpellAffinity { SpellNumber = 1 });
        return t;
    }

    [Fact]
    public void AListedCreatureMatches() {
        SpellAffinityTable t = Table();

        Assert.True(t.Lists(0, 0));
        Assert.True(t.Lists(0, 5));
        // 47 is the last bit of the 3-word mask; an off-by-one in the width drops exactly this one.
        Assert.True(t.Lists(0, 47));
        Assert.False(t.Lists(0, 6));
    }

    [Fact]
    public void ASpellWithAnEmptyMaskMatchesNothing() {
        Assert.False(Table().Lists(1, 0));
    }

    [Fact]
    public void OutOfRangeReadsFalse_RatherThanThrowing() {
        // The original indexes an allocation sized from the file's own count, so a spell number past
        // the end simply never matches. Throwing here would turn a data question into a crash.
        SpellAffinityTable t = Table();

        Assert.False(t.Lists(99, 0));
        Assert.False(t.Lists(-1, 0));
        Assert.False(t.Lists(0, SpellAffinityTable.CreatureTypeCount));
        Assert.False(t.Lists(0, -1));
    }
}
