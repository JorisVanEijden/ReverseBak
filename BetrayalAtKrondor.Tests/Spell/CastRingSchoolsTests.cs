namespace BetrayalAtKrondor.Tests.Spell;

using GameData.Resources.Spells;
using System.Collections.Generic;
using Xunit;

/// <summary>The ring offers a school only when the caster has something to cast in it.</summary>
public class CastRingSchoolsTests {
    private static SpellBookPage Page() {
        var page = new SpellBookPage("invspell.dat");
        page.Groups.Add(Group(37, 9, 44, 4));
        page.Groups.Add(Group(36, 3, 13, 21));
        page.Groups.Add(Group(38, 32, 28));
        return page;
    }

    private static SpellBookGroup Group(int icon, params int[] spellIds) {
        var group = new SpellBookGroup { Icon = icon };
        foreach (int id in spellIds) {
            group.Spells.Add(new SpellBookEntry { SpellId = id, Name = "s" + id });
        }
        return group;
    }

    [Fact]
    public void ACasterWhoKnowsNothingIsOfferedNoSchoolAtAll() =>
        Assert.Empty(CastRingSchools.Available(Page(), SpellBook.Empty()));

    [Fact]
    public void OneSpellOpensExactlyItsOwnSchool() {
        ushort[] known = SpellBook.Empty();
        SpellBook.Learn(known, 13);   // second group

        Assert.Equal(new[] { 1 }, CastRingSchools.Available(Page(), known));
    }

    [Fact]
    public void SchoolsComeBackInPageOrder() {
        ushort[] known = SpellBook.Empty();
        SpellBook.Learn(known, 28);   // third group
        SpellBook.Learn(known, 9);    // first

        Assert.Equal(new[] { 0, 2 }, CastRingSchools.Available(Page(), known));
    }

    [Fact]
    public void KnowsIsFalseForAnEmptyOrAbsentGroup() {
        Assert.False(CastRingSchools.Knows(null, SpellBook.Empty()));
        Assert.False(CastRingSchools.Knows(new SpellBookGroup { Icon = 1 }, SpellBook.Empty()));
    }

    /// <summary>A missing bitmask is "knows nothing", not a crash on the way to the ring.</summary>
    [Fact]
    public void ANullBitmaskOffersNothing() =>
        Assert.Empty(CastRingSchools.Available(Page(), null));
}
