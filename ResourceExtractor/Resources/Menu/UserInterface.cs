namespace ResourceExtractor.Resources.Menu;

public class UserInterface : IResource {
    public int XPosition { get; set; }
    public int YPosition { get; set; }
    public UserInterfaceType UserInterfaceType { get; set; }
    public bool IsModal { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public int Color { get; set; } // 0, 4, 144, 169
    public UiElement[] MenuEntries { get; set; } = Array.Empty<UiElement>();

    public ResourceType Type {
        get => ResourceType.REQ;
    }

    public string? Title { get; set; }
}