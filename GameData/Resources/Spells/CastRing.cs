namespace GameData.Resources.Spells;

/// <summary>
/// RING.DAT — the 30 positions of the spell-casting ring. The casting interface arranges its
/// icons around a fixed elliptical ring of 30 slots; every 5th slot is a spell-category anchor
/// (6 categories x 5 = 30), one per SYMBOL&lt;n&gt;.DAT. Reversed from IDA ovr173
/// (ReadSymbolsAndRingDat @0x6900c, UI_drawCastRingIcons @0x6910e,
/// UI_GetRingPositionAtMouse @0x69690). See docs/FileFormats/RING.DAT.md.
/// </summary>
public class CastRing : IResource {
    public CastRing(string id) { Id = id; }

    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }

    public List<RingPosition> Positions { get; set; } = [];
}

/// <summary>One position on the casting ring.</summary>
public class RingPosition {
    /// <summary>X of the position, in the canonical 1600x1200 display space (scaled from 320x200 VGA).</summary>
    public int X { get; set; }

    /// <summary>Y of the position, in the canonical 1600x1200 display space (scaled from 320x200 VGA).</summary>
    public int Y { get; set; }

    /// <summary>True for the 6 spell-category anchor slots (indices 4, 9, 14, 19, 24, 29).</summary>
    public bool IsCategoryAnchor { get; set; }
}
