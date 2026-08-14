namespace GameData.Resources.Spells;

using System.Collections.Generic;
using System.Text;

/// <summary>
/// What a character's spellbook page actually shows — <c>charscreen_draw_spell_book_actor</c>
/// (<c>SRC/CHAR/CHARSCRN.C</c>).
///
/// <para>The page is <b>six category rows</b>, in <c>INVSPELL.DAT</c>'s own order, each with its
/// icon and a comma-separated list of the spells <i>this character</i> knows from that category.
/// The grouping is the spellbook's own, not a field on the spell records — the same six categories
/// the cast ring and the six SYMBOL files use.</para>
/// </summary>
public static class SpellBookPageView {
    /// <summary>Separator between spell names on a row.</summary>
    public const string Separator = ", ";

    /// <summary>
    /// The spells from one group that a character knows, in the group's own order.
    ///
    /// <para>Order comes from <c>INVSPELL.DAT</c> and is not re-sorted — the original walks the
    /// file and appends as it goes, so a page lists them in file order rather than alphabetically
    /// or by spell id.</para>
    /// </summary>
    public static IReadOnlyList<SpellBookEntry> KnownIn(SpellBookGroup group, ushort[] knownSpells) {
        var known = new List<SpellBookEntry>();
        if (group?.Spells == null) {
            return known;
        }
        foreach (SpellBookEntry entry in group.Spells) {
            if (SpellBook.IsKnown(knownSpells, entry.SpellId)) {
                known.Add(entry);
            }
        }
        return known;
    }

    /// <summary>
    /// One row's text: the known spell names joined with <see cref="Separator"/>.
    /// </summary>
    /// <returns>
    /// An empty string when the character knows none of the group. <b>The row is still drawn</b> —
    /// the original always paints the box and its icon and simply writes nothing beside them, so a
    /// caster who knows one school still sees all six categories.
    /// </returns>
    public static string Line(SpellBookGroup group, ushort[] knownSpells) {
        var text = new StringBuilder();
        foreach (SpellBookEntry entry in KnownIn(group, knownSpells)) {
            if (text.Length > 0) {
                text.Append(Separator);
            }
            text.Append(entry.Name);
        }
        return text.ToString();
    }

    /// <summary>
    /// Every row of the page, in order — one per group, whether or not it has anything to show.
    /// </summary>
    public static IReadOnlyList<string> Lines(SpellBookPage page, ushort[] knownSpells) {
        var lines = new List<string>();
        if (page?.Groups == null) {
            return lines;
        }
        foreach (SpellBookGroup group in page.Groups) {
            lines.Add(Line(group, knownSpells));
        }
        return lines;
    }

    /// <summary>
    /// Whether this character gets a spellbook page at all.
    ///
    /// <para>Gated on <see cref="SpellCasting.IsCaster"/> — the casting skill's <b>maximum</b>, so a
    /// caster drained to nothing still has a book. A non-caster's page is not drawn empty; it is not
    /// drawn.</para>
    /// </summary>
    public static bool HasPage(int castingSkillMaximum) =>
        SpellCasting.IsCaster(castingSkillMaximum);
}
