namespace ResourceExtractor.Resources.Menu;

public class UserInterface : IResource {
    public int XPosition { get; set; }
    public int YPosition { get; set; }
    public UserInterfaceType UserInterfaceType { get; set; }
    public bool Modal { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public int Unknown3 { get; set; } // 0, 2, 3, 4, 8, 28
    public uint Unknown4 { get; set; } // Always 0
    public int Unknown0 { get; set; } // 0, 4, 144, 169
    public UiElement[] MenuEntries { get; set; } = Array.Empty<UiElement>();

    public ResourceType Type {
        get => ResourceType.REQ;
    }
}