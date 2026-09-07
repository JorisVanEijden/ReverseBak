namespace BetrayalAtKrondor.Tests.Spells;

using System;
using System.IO;
using System.Linq;
using System.Text;
using global::GameData.Resources.Dialog;
using global::GameData.Resources.Spells;
using global::ResourceExtraction.Extractors.Dialog;
using Xunit;

/// <summary>
/// Each timed field spell's narrative is the record the port sends it to.
/// </summary>
/// <remarks>
/// <b>The mapping is POSITIONAL, which is why it needs pinning against the shipped text.</b>
/// <see cref="FieldSpells.DialogFor"/> is <c>199 + EventIdOf(spell)</c>, and <c>EventIdOf</c> is the
/// spell's INDEX IN <see cref="FieldSpells.All"/>. So reordering that array — a harmless-looking
/// edit, since it is documented only as "the order the dispatcher's table lists them" — silently
/// sends every spell to another spell's narrative. Nothing throws and every dialog still displays.
///
/// <para>Asserted against a distinctive phrase from each shipped record rather than against six
/// numbers, because six numbers would agree with a reordered array just as happily. The records are
/// self-identifying — Dragon's Breath rolls fog from the caster's mouth, Stardusk flares the stars
/// overhead — so matching text to spell is a real check on the arithmetic between them.</para>
///
/// <para>The three locators are excluded on purpose: <c>EventIdOf</c> returns -1 for them and they
/// show a map or the "complete waste of time" line instead. That exclusion is asserted too, since
/// giving them a slot would shift the six.</para>
/// </remarks>
public class FieldSpellNarrativeTests {
    static FieldSpellNarrativeTests() =>
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private const string NarrativeFile = "DIAL_Z00.DDX";

    private static Dialog? LoadShippedDialogs() {
        string? dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            string path = Path.Combine(dir, "OriginalGame", NarrativeFile);
            if (File.Exists(path)) {
                using FileStream stream = File.OpenRead(path);
                return new DdxExtractor().Extract(NarrativeFile, stream);
            }

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }

    private static string TextOf(Dialog dialogs, int id) =>
        dialogs.Entries.FirstOrDefault(e => e.Id == (uint)id)?.Text ?? string.Empty;

    [Theory]
    [InlineData(FieldSpells.DragonsBreath, "fog were rolling out")]
    [InlineData(FieldSpells.CandleGlow, "artificial light dissolved the darkness")]
    [InlineData(FieldSpells.Stardusk, "the stars flared")]
    [InlineData(FieldSpells.AndTheLightShallLie, "faint glow")]
    [InlineData(FieldSpells.Union, "shifting in his mind")]
    [InlineData(FieldSpells.ScentOfSarig, "invisible to all but")]
    public void EachTimedSpellReachesItsOwnNarrative(int spellId, string phrase) {
        Dialog? dialogs = LoadShippedDialogs();
        if (dialogs == null) {
            return;
        }

        int record = FieldSpells.DialogFor(spellId);
        Assert.InRange(record, FieldSpells.FirstTimedDialog, FieldSpells.FirstTimedDialog + 5);
        Assert.Contains(phrase, TextOf(dialogs, record), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A locator has no narrative, and taking a slot would shift every spell after it.</summary>
    [Fact]
    public void TheLocatorsTakeNoNarrativeSlot() {
        foreach (int locator in new[] {
                     FieldSpells.EyesOfIshap, FieldSpells.TheUnseen, FieldSpells.NacreCicatrix }) {
            Assert.True(FieldSpells.IsLocatorRoll(locator));
            Assert.Equal(-1, FieldSpells.DialogFor(locator));
        }
    }

    /// <summary>The six that do take one take six consecutive records, no gaps and no repeats.</summary>
    [Fact]
    public void TheSixTimedSpellsFillTheRunExactly() {
        int[] records = FieldSpells.All
            .Where(id => !FieldSpells.IsLocatorRoll(id))
            .Select(FieldSpells.DialogFor)
            .OrderBy(record => record)
            .ToArray();

        Assert.Equal(Enumerable.Range(FieldSpells.FirstTimedDialog, 6), records);
    }
}
