namespace GameData.Resources.Spells;

using System.Collections.Generic;

/// <summary>
/// INVSPELL.DAT — how the character sheet's spellbook page is laid out: six rows, each an icon and
/// the spells that belong under it.
///
/// <para>Read by <c>charscreen_draw_spell_book_actor</c> (canassa <c>SRC/CHAR/CHARSCRN.C</c>), and
/// only for a character who can cast. For each of the six rows the engine draws the row's icon and
/// then a comma-separated list of just those spells the character actually knows, tested as
/// <c>pSpellsKnown[id / 16] &amp; (1 &lt;&lt; (id % 16))</c>. So this file supplies the page's
/// vocabulary and grouping; which entries appear is per-character.</para>
///
/// <para>The six rows line up with the six category anchors on the casting ring, which is the other
/// place the same grouping shows up.</para>
/// </summary>
public class SpellBookPage : IResource {
    public SpellBookPage(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>The six rows, in file order — which is the order they are drawn down the page.</summary>
    public List<SpellBookGroup> Groups { get; set; } = new List<SpellBookGroup>();
}

/// <summary>One row of the spellbook page.</summary>
public class SpellBookGroup {
    /// <summary>Index into the button-sprite set, drawn at the left of the row.</summary>
    public int Icon { get; set; }

    /// <summary>The spells filed under this row, in file order.</summary>
    public List<SpellBookEntry> Spells { get; set; } = new List<SpellBookEntry>();
}

/// <summary>A spell's entry on the page: the name as printed, and which spell it is.</summary>
public class SpellBookEntry {
    /// <summary>Display name, as stored (a fixed 24-byte field, NUL-padded).</summary>
    public string Name { get; set; } = "";

    /// <summary>Spell id — the bit index into the character's known-spell bitmask.</summary>
    public int SpellId { get; set; }

    /// <summary>De-indexed <see cref="SpellId"/>: <c>base:spell:&lt;id&gt;</c>.</summary>
    public string SpellKey { get; set; } = "";
}
