namespace GameData.Resources.Menu;

[Serializable]
public class UserInterface : IResource {
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
    public int ColorBase { get; set; } // base index into the 7-color palette range used by the renderer; 169 = default fullscreen-menu set
    public UiElement[] MenuEntries { get; set; } = [];

    public ResourceType Type {
        get => ResourceType.REQ;
    }

    public string Id { get; }

    public string? Title { get; set; }
}