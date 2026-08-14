namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The spellbook page: six category rows, each listing what this character knows from that
/// category. The empty-row and ordering behaviours are the ones a port drifts on.
/// </summary>
public class SpellBookPageViewTests {
    private static SpellBookGroup Group(int icon, params (int Id, string Name)[] spells) {
        var group = new SpellBookGroup { Icon = icon };
        foreach ((int id, string name) in spells) {
            group.Spells.Add(new SpellBookEntry { SpellId = id, Name = name });
        }
        return group;
    }

    private static SpellBookPage Page() {
        var page = new SpellBookPage("invspell.dat");
        page.Groups.Add(Group(37, (9, "Bane of Black Slayers"), (44, "Evil Seek"), (4, "Flamecast")));
        page.Groups.Add(Group(36, (2, "Candle Glow")));
        page.Groups.Add(Group(38));
        return page;
    }

    private static ushort[] Knowing(params int[] spellIds) {
        ushort[] words = SpellBook.Empty();
        foreach (int id in spellIds) {
            SpellBook.Learn(words, id);
        }
        return words;
    }

    [Fact]
    public void ARowListsOnlyWhatThisCharacterKnows() {
        SpellBookPage page = Page();

        Assert.Equal("Bane of Black Slayers, Flamecast",
            SpellBookPageView.Line(page.Groups[0], Knowing(9, 4, 2)));
    }

    [Fact]
    public void TheOrderIsTheFilesNotAlphabeticalOrById() {
        // The original walks INVSPELL.DAT and appends as it goes.
        SpellBookPage page = Page();

        Assert.Equal("Bane of Black Slayers, Evil Seek, Flamecast",
            SpellBookPageView.Line(page.Groups[0], Knowing(4, 9, 44)));
    }

    [Fact]
    public void KnowingNothingInACategoryLeavesTheRowEmptyButPresent() {
        // The box and its icon are still drawn — a caster who knows one school still sees all six
        // categories, not a shortened page.
        SpellBookPage page = Page();
        IReadOnlyList<string> lines = SpellBookPageView.Lines(page, Knowing(2));

        Assert.Equal(3, lines.Count);
        Assert.Equal(string.Empty, lines[0]);
        Assert.Equal("Candle Glow", lines[1]);
        Assert.Equal(string.Empty, lines[2]);
    }

    [Fact]
    public void AnEmptyCategoryInTheDataStillGetsARow() {
        SpellBookPage page = Page();

        Assert.Equal(string.Empty, SpellBookPageView.Line(page.Groups[2], Knowing(9, 4, 2, 44)));
    }

    [Fact]
    public void KnowingNothingAtAllStillYieldsEveryRow() {
        Assert.Equal(3, SpellBookPageView.Lines(Page(), SpellBook.Empty()).Count);
    }

    [Fact]
    public void TheKnownEntriesComeBackAsRecordsNotJustText() {
        IReadOnlyList<SpellBookEntry> known =
            SpellBookPageView.KnownIn(Page().Groups[0], Knowing(44));

        Assert.Single(known);
        Assert.Equal(44, known[0].SpellId);
    }

    [Fact]
    public void OnlyACasterHasAPageAtAll() {
        // The maximum, so a drained caster keeps their book; a non-caster's page is not drawn empty
        // — it is not drawn.
        Assert.True(SpellBookPageView.HasPage(30));
        Assert.False(SpellBookPageView.HasPage(0));
    }

    [Fact]
    public void NullsAnswerEmptyRatherThanThrowing() {
        Assert.Empty(SpellBookPageView.Lines(null, SpellBook.Empty()));
        Assert.Equal(string.Empty, SpellBookPageView.Line(null, SpellBook.Empty()));
        Assert.Empty(SpellBookPageView.KnownIn(new SpellBookGroup(), null));
    }
}
