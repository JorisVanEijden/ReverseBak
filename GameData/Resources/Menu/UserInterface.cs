namespace GameData.Resources.Menu;

[Serializable]
public class UserInterface : IResource {
    /// <summary>
    /// ActionId of a synthesized, data-only marker element the extractor adds to REQ_MAIN for the
    /// travel-HUD compass window. The original REQ_MAIN has no compass element — the window is a
    /// fixed FRAME.SCR design rect (drawCompass @ KRONDOR.EXE 0x4691f: VGA 144,121,31,10). Emitting it
    /// as a REQ element (ElementType.Unknown, so no renderer touches it) lets the Unity CompassView
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

    public ResourceType Type {
        get => ResourceType.REQ;
    }

    public string Id { get; }

    public string? Title { get; set; }
}