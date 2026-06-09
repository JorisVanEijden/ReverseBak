namespace GameData.Resources.Cursor;

/// <summary>One cursor frame: its index in the set, canonical pixel size, and hotspot.
/// The hotspot is RE-derived in CursorExtractor from the original SetPointerImage rule
/// (index 0/1 -> top-left (0,0); index >= 2 -> centred).</summary>
public class CursorImage {
    public int Index { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int HotspotX { get; set; }
    public int HotspotY { get; set; }
}
