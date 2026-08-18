namespace GameData.Resources.Inventory;

/// <summary>
/// Using a note to look at a map — <c>ITEMUSE.C</c>'s category-16 branch.
///
/// <para><b>Almost none of it is about maps.</b> Notes are a whole item category, but exactly one
/// note in the game shows anything, and that one carries the map it shows in its condition byte.
/// Everything else in the category answers with a line of dialog and no picture.</para>
/// </summary>
public static class NoteMapView {
    /// <summary>The one note item that shows a map.</summary>
    public const int MapNoteItemId = 120;

    /// <summary>
    /// The only map id with an image behind it.
    /// </summary>
    /// <remarks>
    /// The id lives in the item's <b>condition</b> byte, the same field a scroll keeps its spell
    /// number in — a general pattern in this data, not a coincidence here.
    /// </remarks>
    public const int RiftMapId = 32;

    /// <summary>Save-state key of the "this map has been looked at" flag: <c>mapId + 6500</c>.</summary>
    public const int MapViewedFlagBase = 6500;

    /// <summary>
    /// The one zone in which the map marks where you are.
    /// </summary>
    /// <remarks>
    /// <b>Everywhere else the map is drawn with no marker at all.</b> The party position is computed
    /// unconditionally and then only painted in this zone, so a port that always draws the marker
    /// shows the player a confident position on a map that, in the original, tells them nothing.
    /// </remarks>
    public const int MarkerZone = 9;

    /// <summary>Dialog when the item is a note but not the one with a map.</summary>
    public const int WrongNoteDialogId = 0x1b7742;

    /// <summary>
    /// Dialog played before an unseen map, and instead of one when the note names a map that has no
    /// image.
    /// </summary>
    public const int PrefaceDialogId = 0x1b7753;

    /// <summary>Dialog that holds the map on screen.</summary>
    public const int MapShownDialogId = 0x1b7772;

    /// <summary>Background the map is drawn on.</summary>
    public const string MapBackground = "RIFTMAP.SCX";

    /// <summary>Whether using this item shows a map at all.</summary>
    public static bool ShowsAMap(int itemId) => itemId == MapNoteItemId;

    /// <summary>Whether a map id has an image to show.</summary>
    public static bool HasImage(int mapId) => mapId == RiftMapId;

    /// <summary>The flag key for a map id.</summary>
    public static int ViewedFlag(int mapId) => MapViewedFlagBase + mapId;

    /// <summary>
    /// Whether the map has been looked at before — the preface line plays only the first time.
    /// </summary>
    public static bool NeedsPreface(int viewedFlagValue) => viewedFlagValue == 0;

    /// <summary>
    /// <b>The map is marked viewed whichever way the branch went.</b>
    /// </summary>
    /// <remarks>
    /// The write sits outside the test on the map id, so reading a note whose map has no image
    /// still records that map as seen. Nothing reads it back for those ids, but a port that only
    /// sets the flag on a successful display would diverge from the save's contents.
    /// </remarks>
    public static bool MarksViewedEvenWithNoImage => true;

    /// <summary>Whether to paint the "you are here" marker.</summary>
    public static bool ShowsMarker(int zoneId) => zoneId == MarkerZone;

    /// <summary>The palette pen the marker is painted in (0x59299).</summary>
    public const int MarkerPen = 0x6c;

    /// <summary>Marker width.</summary>
    public const int MarkerWidth = 12;

    /// <summary>Marker height.</summary>
    public const int MarkerHeight = 10;

    /// <summary>World origin both axes are measured from.</summary>
    private const int WorldOrigin = 640000;

    /// <summary>World units per map pixel across.</summary>
    private const int WorldPerPixelX = 0x8f7;

    /// <summary>World units per map pixel down.</summary>
    private const int WorldPerPixelY = 0x92a;

    private const int MapOriginX = 0x90;

    private const int MapBottomY = 0xc0;

    /// <summary>The party's column on the map.</summary>
    public static int MapX(long worldX) => (int)(((worldX - WorldOrigin) / WorldPerPixelX) + MapOriginX);

    /// <summary>
    /// The party's row on the map.
    /// </summary>
    /// <remarks>
    /// <b>Inverted:</b> the map's rows run opposite to the world's Y, so this subtracts rather than
    /// adds. The two axes are also scaled by <i>different</i> divisors, so the map is not square to
    /// the world — using one divisor for both puts the marker increasingly wrong the further from
    /// the origin you are.
    /// </remarks>
    public static int MapY(long worldY) => (int)(MapBottomY - ((worldY - WorldOrigin) / WorldPerPixelY));

    /// <summary>Top-left of the marker rectangle, which is centred on the computed point.</summary>
    public static (int X, int Y) MarkerTopLeft(long worldX, long worldY) =>
        (MapX(worldX) - (MarkerWidth / 2), MapY(worldY) - (MarkerHeight / 2));
}
