namespace GameData.Resources.Spells;

/// <summary>
/// The spell-selection layout for one spell category (SYMBOL1.DAT .. SYMBOL6.DAT).
/// Each file lists the castable spells of that category as positioned, clickable
/// glyph nodes shown on the casting screen. Reversed from IDA ovr173
/// (ReadSymbolsAndRingDat @0x6900c, UI_drawSpellSymbols @0x69252,
/// UI_GetSymbolAtMouse @0x69192). See docs/FileFormats/SYMBOLx.DAT.md.
/// </summary>
public class SpellSymbolLayout : IResource {
    public SpellSymbolLayout(string id) { Id = id; }

    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }

    /// <summary>Zero-based spell category, derived from the filename (SYMBOL1 -> 0 .. SYMBOL6 -> 5).</summary>
    public int Category { get; set; }

    public List<SpellSymbolNode> Nodes { get; set; } = [];
}

/// <summary>One castable spell node in a <see cref="SpellSymbolLayout"/>.</summary>
public class SpellSymbolNode {
    /// <summary>Index into the spell table (SPELLS.DAT); selects which spell this node casts.</summary>
    public int SpellId { get; set; }

    /// <summary>De-indexed <see cref="SpellId"/>: <c>base:spell:&lt;SpellId&gt;</c>, the spell this node
    /// casts. See docs/re-notes/reference-inventory.md #9.</summary>
    public string SpellKey { get; set; } = "";

    /// <summary>X of the glyph centre, in the canonical 1600x1200 display space (scaled from 320x200 VGA).</summary>
    public int X { get; set; }

    /// <summary>Y of the glyph centre, in the canonical 1600x1200 display space (scaled from 320x200 VGA).</summary>
    public int Y { get; set; }

    /// <summary>
    /// SPELL.FNT glyph index drawn for this node (the magic-symbol shape). The on-disk byte is
    /// stored as <c>glyph - 1</c>; the engine adds 1 at load time, and this value has that already applied.
    /// </summary>
    public int FontGlyph { get; set; }
}
