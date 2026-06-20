namespace GameData.Resources.Combat;

/// <summary>
/// Engine-independent view of GRID.DAT — the per-zone <b>combat-grid border colour</b>.
/// The combat screen draws the outline of the playable tactical-grid region in a colour
/// that varies by zone (so the grid outline blends with each zone's combat backdrop).
///
/// On-disk layout: 24 bytes = <see cref="ZoneCount"/> × u16, one pen index per zone,
/// indexed <c>zoneNumber - 1</c>. <c>Load_grid</c> (seg033 @ 0x2e7ee) seeks to
/// <c>(currentZoneNumber - 1) * 2</c> and reads a single word into the global
/// <c>grid_dat_word_3E840</c>; <c>sub_seg033_12B4</c> (0x2e8a4) then walks the grid,
/// finds the boundary cells (combat-grid element 7), and draws the outline segments with
/// that colour via the line drawer <c>sub_seg034_129D</c>.
///
/// The value is a <b>palette pen index</b> into the zone's active combat palette, not an
/// RGB colour — resolving it to RGB needs that palette (a separate resource selected at
/// combat-start), so it is left as an index here. See <c>docs/FileFormats/GRID.DAT.md</c>.
/// </summary>
public class GridData : IResource {
    /// <summary>Number of per-zone entries (zone numbers 1..12).</summary>
    public const int ZoneCount = 12;

    public GridData(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>Combat-grid border pen index per zone, indexed by <c>zoneNumber - 1</c>
    /// (a palette index into the zone's combat palette).</summary>
    public List<int> ZoneBorderPens { get; set; } = new();
}
