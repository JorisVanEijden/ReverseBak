namespace GameData.Resources.Config;

/// <summary>
/// ENCAMP.DAT — geometry for the encampment / "rest" screen (ENCAMP.SCX), reversed from
/// <c>Load_encamp</c> (ovr182 @ 0x7087e). Structurally a twin of FMAP_TWN.DAT: four
/// icon-geometry header words followed by two (x, y) point arrays.
///
/// On disk the coordinates are 320×200 mode-13h pixels; the extractor scales them into the
/// canonical 1600×1200 space (×5 / ×6 via <c>AspectCorrection</c>), like FMAP, so they line
/// up with ENCAMP.SCX and the REQ/GDS/Label coordinate space.
///
/// File layout (216 bytes shipped):
///   +0x00 u16 ×4   icon geometry: IconAnchorX, IconAnchorY, IconWidth, IconHeight (3,3,9,9)
///   +0x08 u16      clock-entry count (24), then that many (x, y) u16 pairs
///   then  u16      needle-entry count (27), then that many (x, y) u16 pairs
///
/// <see cref="ClockEntries"/> are the clickable hour positions on the rest dial. The
/// hit-test (<c>encamp_clock?_sub_ovr182_75F</c> @ 0x70b2f) builds the box
/// <c>[X-(IconWidth-IconAnchorX)/2 .. +IconWidth] × [Y-(IconHeight-IconAnchorY)/2 .. +IconHeight]</c>
/// around each point and returns the clock entry under the cursor.
/// <see cref="NeedleEntries"/> are NOT a needle, despite the name this file was reversed under:
/// <c>encamp_drawSundialShadow</c> (@ 0x70c9b) fills a THREE-point polygon from them — entry 0 is
/// the gnomon's tip, entry 1 the dial's centre, entry 2 a scratch slot, and entries 3..26 are the
/// 24 arc points the third vertex sweeps through the daylight hours. See <see cref="EncampDial"/>.
/// </summary>
public class EncampData : IResource {
    public EncampData(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>Icon centring anchor X (canonical space). Shipped 3 → 15.</summary>
    public int IconAnchorX { get; set; }

    /// <summary>Icon centring anchor Y (canonical space). Shipped 3 → 18.</summary>
    public int IconAnchorY { get; set; }

    /// <summary>Clock-entry hit-box width (canonical space). Shipped 9 → 45.</summary>
    public int IconWidth { get; set; }

    /// <summary>Clock-entry hit-box height (canonical space). Shipped 9 → 54.</summary>
    public int IconHeight { get; set; }

    /// <summary>Clickable hour positions on the rest dial (24 shipped), canonical space.</summary>
    public List<EncampPoint> ClockEntries { get; set; } = new();

    /// <summary>Sundial-shadow polygon vertices (27 shipped), canonical space — see the type's
    /// own remarks for what the three groups are.</summary>
    public List<EncampPoint> NeedleEntries { get; set; } = new();

    /// <summary>Nothing under the cursor.</summary>
    public const int NoEntry = -1;

    /// <summary>
    /// The hour whose dial position is under a point, or <see cref="NoEntry"/>.
    /// Faithful port of <c>encamp_getClockEntryAtMouse</c> @0x70b2f.
    /// </summary>
    /// <remarks>
    /// <b>The hit box is NOT centred on the dial position.</b> Its left edge sits
    /// <c>(IconWidth - IconAnchorX) / 2</c> to the left, and it then runs a full
    /// <see cref="IconWidth"/> to the right — so with the shipped geometry it reaches 15 canonical
    /// units left of the point and 30 to the right. Centring it, which is what the numbers invite,
    /// shifts every hour's target and makes the dial read as misaligned with its own artwork.
    ///
    /// <para>Both edges are inclusive, so the box is one unit wider and taller than
    /// <see cref="IconWidth"/> by <see cref="IconHeight"/>. First match in table order wins; the
    /// shipped positions do not overlap, so the tie-break never arises, but it is the original's
    /// and costs nothing to keep.</para>
    /// </remarks>
    public int ClockEntryAt(int x, int y) {
        int halfWidth = (IconWidth - IconAnchorX) / 2;
        int halfHeight = (IconHeight - IconAnchorY) / 2;

        for (var entry = 0; entry < ClockEntries.Count; entry++) {
            int left = ClockEntries[entry].X - halfWidth;
            int top = ClockEntries[entry].Y - halfHeight;
            if (x >= left && x <= left + IconWidth && y >= top && y <= top + IconHeight) {
                return entry;
            }
        }

        return NoEntry;
    }
}

/// <summary>A 2D point in canonical 1600×1200 space.</summary>
public class EncampPoint {
    public int X { get; set; }
    public int Y { get; set; }
}
