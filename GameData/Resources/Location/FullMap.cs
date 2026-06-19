namespace GameData.Resources.Location;

using System.Collections.Generic;

/// <summary>
/// FMAP_TWN.DAT — the town labels drawn on the world map screen (FULLMAP.SCX).
/// Each town has a name and a position, reversed from <c>Load_fmap_twn.dat</c>
/// (ovr183 @ 0x713cd). On disk the coordinates are 320×200 mode-13h pixels; the
/// extractor scales them into the <b>canonical 1600×1200 space</b> (×5 / ×6, via
/// <c>AspectCorrection</c>) so they line up with the FULLMAP.SCX background and
/// the REQ/GDS/Label coordinate space.
///
/// The four header words are shared icon/label geometry constants (the shipped
/// file has 3,3,9,9). They are consumed by:
///   * <c>UI_full_map</c> (0x70f60) — the label of town i is centred at
///     <c>X + IconAnchorX/2</c> horizontally (minus half the text width) and
///     drawn one label-height above <c>Y</c>.
///   * <c>sub_ovr183_652</c> (0x715b2) — the mouse hit-test builds the icon
///     rectangle <c>[X-(IconWidth-IconAnchorX)/2 .. +IconWidth]</c> ×
///     <c>[Y-(IconHeight-IconAnchorY)/2 .. +IconHeight]</c> and returns the
///     town under the cursor.
/// So (IconWidth, IconHeight) is the FMAP_ICN icon footprint and
/// (IconAnchorX, IconAnchorY) the centring anchor.
/// </summary>
public class FullMapTowns : IResource {
    public FullMapTowns(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    // Icon/label geometry, scaled into canonical 1600×1200 space.
    public int IconAnchorX { get; set; }
    public int IconAnchorY { get; set; }
    public int IconWidth { get; set; }
    public int IconHeight { get; set; }

    public List<FullMapTown> Towns { get; set; } = new();
}

public class FullMapTown {
    public string Name { get; set; } = string.Empty;

    /// <summary>Town label X in canonical 1600×1200 space.</summary>
    public int X { get; set; }

    /// <summary>Town label Y in canonical 1600×1200 space.</summary>
    public int Y { get; set; }
}

/// <summary>
/// FMAP_XY.DAT — maps the player's (zone, world-tile) to a "you are here"
/// marker position on the world map screen, reversed from
/// <c>Load_fmap_xy.dat</c> (ovr183 @ 0x71634).
///
/// The file is exactly 12 zones (zone numbers 1..12), each a count followed by
/// that many (x, y) marker positions — one per world tile in the zone. On disk
/// these are 320×200 mode-13h pixels; the extractor scales them into the
/// <b>canonical 1600×1200 space</b> (×5 / ×6). The loader looks up
/// <c>Zones[currentZone-1].Markers[currentTile]</c>; the on-disk (-1,-1) "no
/// marker" sentinel is surfaced as a <c>null</c> list entry rather than a magic
/// coordinate, so consumers needn't know about it.
/// </summary>
public class FullMapPositions : IResource {
    public const int ZoneCount = 12;

    public FullMapPositions(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>Indexed [0..11] for zone numbers 1..12.</summary>
    public List<FullMapZone> Zones { get; set; } = new();
}

public class FullMapZone {
    /// <summary>
    /// One entry per world tile in this zone, indexed by tile number. Each entry
    /// is the marker position in canonical 1600×1200 space, or <c>null</c> when
    /// the tile has no map marker (the on-disk (-1,-1) sentinel).
    /// </summary>
    public List<MapMarker?> Markers { get; set; } = new();
}

public class MapMarker {
    public int X { get; set; }
    public int Y { get; set; }
}
