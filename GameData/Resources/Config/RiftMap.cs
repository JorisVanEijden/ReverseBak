namespace GameData.Resources.Config;

/// <summary>
/// The rift map a Note shows when it is used — <c>Use_Item</c> @0x591f8 (ovr161).
/// </summary>
/// <remarks>
/// <b>Not every Note is the rift map.</b> The branch is gated on the item's own variable being
/// <see cref="RiftMapNoteVariable"/>; any other Note falls through to a different message. So this
/// is one specific note, not the Note category.
///
/// <para>The map itself is the static screen RIFTMAP.SCR — it is not the interactive local map
/// (REQ_MAP), which is a different resource with its own navigation.</para>
///
/// <para>Positions are canonical 1600x1200 (VGA x5 across, x6 down), matching
/// <see cref="InnScreenLayout"/>.</para>
/// </remarks>
public static class RiftMap {
    /// <summary>The Note variable that makes a note THE rift map (0x591fa).</summary>
    public const int RiftMapNoteVariable = 32;

    /// <summary>The full-screen map image.</summary>
    public const string ScreenResource = "RIFTMAP.SCR";

    /// <summary>
    /// Set once the party has the spy notes. Without it they are shown first.
    /// </summary>
    /// <remarks>
    /// The dialog is shown and the map is STILL displayed — the notes are context, not a lock.
    /// </remarks>
    public const int SpyNotesGlobal = 6532;

    /// <summary>Shown before the map when <see cref="SpyNotesGlobal"/> is unset.</summary>
    public const int SpyNotesDialog = 1800019;

    /// <summary>Shown over the map every time.</summary>
    public const int UsingMapDialog = 1800050;

    /// <summary>
    /// The one zone whose position is marked on the map.
    /// </summary>
    /// <remarks>
    /// <b>The marker is drawn in zone 9 and nowhere else</b> (0x59295). Everywhere else the map is
    /// shown with no "you are here" at all — which is the zone-dependence this screen is described
    /// by, and not a per-zone image.
    /// </remarks>
    public const int MarkerZone = 9;

    /// <summary>The marker box's pen.</summary>
    public const int MarkerPen = 0x6C;

    // The projection's divisors and origin are VGA-space constants from the branch itself; the
    // arithmetic is done in that space and scaled afterwards so the original's integer rounding
    // survives. Doing it in canonical units would round differently and drift the marker.
    private const int WorldOrigin = 640000;
    private const int WorldPerPixelX = 0x8F7;   // 2295
    private const int WorldPerPixelY = 0x92A;   // 2346
    private const int MapOriginXVga = 0x90;     // 144
    private const int MapBottomYVga = 0xC0;     // 192

    /// <summary>Marker box width — VGA 12, centred by <see cref="MarkerOffsetXVga"/>.</summary>
    public const int MarkerWidth = 12 * 5;

    /// <summary>Marker box height — VGA 10.</summary>
    public const int MarkerHeight = 10 * 6;

    private const int MarkerOffsetXVga = 6;
    private const int MarkerOffsetYVga = 5;

    /// <summary>
    /// Where the party sits on the map, in canonical units — the marker box's TOP-LEFT.
    /// </summary>
    /// <remarks>
    /// <b>Y is inverted</b>: the map's origin is at the bottom (<c>192 - y</c>), so walking north
    /// moves the marker up. X is a plain offset from 144. The box is then pulled back by half its
    /// own size so it is centred on the point rather than hanging off it.
    /// </remarks>
    public static (int X, int Y) MarkerTopLeft(int worldX, int worldY) {
        int vgaX = (worldX - WorldOrigin) / WorldPerPixelX + MapOriginXVga;
        int vgaY = MapBottomYVga - (worldY - WorldOrigin) / WorldPerPixelY;

        return ((vgaX - MarkerOffsetXVga) * 5, (vgaY - MarkerOffsetYVga) * 6);
    }

    /// <summary>Whether this note, used in this zone, should carry a position marker.</summary>
    public static bool ShowsMarker(int noteVariable, int zone) =>
        noteVariable == RiftMapNoteVariable && zone == MarkerZone;
}
