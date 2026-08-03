namespace GameData.Resources.Menu;

using GameData.Resources.Inventory;
using GameData.Resources.Layout;

[Serializable]
public class UserInterface : IResource {
    /// <summary>
    /// ActionId of a synthesized, data-only marker element the extractor adds to REQ_MAIN for the
    /// travel-HUD compass window. The original REQ_MAIN has no compass element — the window is a
    /// fixed FRAME.SCR design rect (drawCompass @ KRONDOR.EXE 0x4691f: VGA 144,121,31,10). Emitting it
    /// as a REQ element (ElementType.CompassWindow, so no renderer touches it) lets the Unity CompassView
    /// read its canonical rect via the normal REQ path, keeping VGA knowledge in the extractor only.
    /// </summary>
    public const int CompassWindowActionId = 1000;

    public UserInterface(string id) {
        Id = id;
    }

    public int XPosition { get; set; }
    public int YPosition { get; set; }
    public UserInterfaceType UserInterfaceType { get; set; }
    public bool IsModal { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public Colorset Colorset { get; set; } // semantic selector for the screen's palette range; the Unity theme maps this to actual colours
    public UiElement[] MenuEntries { get; set; } = [];

    /// <summary>
    /// The coordinate space XPosition/YPosition/Width/Height (and every MenuEntries rect) resolve
    /// against. Distinct from the screen's own Width/Height above — e.g. REQ_CAMP's Width is 1470
    /// (a panel), while its Frame is the full 1600x1200 canonical space it's positioned within.
    /// Populated by the extractor from AspectCorrection.CanonicalWidth/Height.
    /// </summary>
    public DesignFrame Frame { get; set; } = new();

    /// <summary>
    /// Loot/inventory grid + paperdoll geometry (<see cref="Resources.Inventory.InventoryLayout"/>).
    /// Null for every REQ except <c>REQ_INV</c> — it is the only screen with a fixed-cell item
    /// grid; <c>REQ_INV2</c> is a distinct screen and does not get one. Populated by
    /// <c>UserInterfaceExtractor</c>, not read from the REQ_INV.DAT bytes (the geometry is
    /// transcribed from immediate operands in the original routine — see
    /// <see cref="Resources.Inventory.InventoryLayout"/> for provenance).
    /// </summary>
    public InventoryLayout? Inventory { get; set; }

    public ResourceType Type {
        get => ResourceType.REQ;
    }

    public string Id { get; }

    public string? Title { get; set; }
}