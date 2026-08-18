namespace GameData.Resources.Spells;

using System.Collections.Generic;

/// <summary>
/// Which of the six casting schools a particular caster can actually be offered.
/// </summary>
/// <remarks>
/// <b>The ring is not a fixed set of six.</b> A school belongs on it only when the caster knows at
/// least one spell in it, so the buttons change with whoever is selected — and a character who has
/// learned nothing gets no schools at all, only the way out.
///
/// <para>The grouping is <c>INVSPELL.DAT</c>'s, the same six groups the character sheet's spellbook
/// page prints, and each group carries the icon its button should wear. So "which schools" and
/// "which icons" are one question with one answer, rather than a list of schools kept in step with
/// a parallel list of icons.</para>
/// </remarks>
public static class CastRingSchools {
    /// <summary>
    /// Whether <paramref name="knownSpells"/> holds any spell from <paramref name="group"/>.
    /// </summary>
    /// <param name="knownSpells">The caster's spell bitmask — see <see cref="SpellBook"/>.</param>
    public static bool Knows(SpellBookGroup group, ushort[] knownSpells) {
        if (group?.Spells == null || knownSpells == null) {
            return false;
        }

        foreach (SpellBookEntry entry in group.Spells) {
            if (SpellBook.IsKnown(knownSpells, entry.SpellId)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The school indices this caster can be offered, in page order.
    /// </summary>
    /// <remarks>
    /// Index is the group's position in the page, which is what
    /// <see cref="CastMenuSelection.SchoolForAction"/> resolves an action id to — so the caller can
    /// match a button to a group without a second mapping to keep honest.
    /// </remarks>
    public static IReadOnlyList<int> Available(SpellBookPage page, ushort[] knownSpells) {
        var open = new List<int>();
        if (page?.Groups == null) {
            return open;
        }

        for (var i = 0; i < page.Groups.Count; i++) {
            if (Knows(page.Groups[i], knownSpells)) {
                open.Add(i);
            }
        }

        return open;
    }
}
