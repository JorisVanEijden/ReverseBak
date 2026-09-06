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
    /// The ring school a spellbook GROUP belongs to — they are not the same ordering.
    /// </summary>
    /// <remarks>
    /// <b>INVSPELL.DAT's group order and the SYMBOL file order agree for the first four and swap
    /// the last two.</b> Compared as SETS of spell ids against the shipped files:
    /// <code>
    ///   group 0 {4,5,9,36,42,44}          == SYMBOL1  -> school 0
    ///   group 1 {3,13,15,19,20,21,22,31}  == SYMBOL2  -> school 1
    ///   group 2 {12,16,25,28,29,30,32,41} == SYMBOL3  -> school 2
    ///   group 3 {1,6,7,14,23,27,37}       == SYMBOL4  -> school 3
    ///   group 4 {0,2,26,34,35}            == SYMBOL6  -> school 5   &lt;-- swapped
    ///   group 5 {8,11,17,18}              == SYMBOL5  -> school 4   &lt;-- swapped
    /// </code>
    ///
    /// <para><b>This class used to say no such mapping was needed</b> — "the caller can match a
    /// button to a group without a second mapping to keep honest". It is needed: a button's action
    /// id resolves through <see cref="CastMenuSelection.SchoolForAction"/> to a SYMBOL index, so
    /// feeding it a group index gave the last two buttons each other's icon and each other's
    /// enabled state, while still selecting the school their position means.</para>
    ///
    /// <para>The swap is its own inverse, so this converts in both directions. Pinned against the
    /// shipped data by a test rather than trusted as a constant.</para>
    /// </remarks>
    public static int SchoolForGroup(int group) => group switch {
        FirstSwappedGroup => SecondSwappedGroup,
        SecondSwappedGroup => FirstSwappedGroup,
        _ => group,
    };

    private const int FirstSwappedGroup = 4;
    private const int SecondSwappedGroup = 5;

    /// <summary>
    /// The school indices this caster can be offered, in page order.
    /// </summary>
    /// <remarks>
    /// <b>Returns GROUP indices.</b> Put each through <see cref="SchoolForGroup"/> before treating
    /// one as a ring school or a button position.
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
