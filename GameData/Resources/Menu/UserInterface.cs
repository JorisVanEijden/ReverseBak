namespace GameData.Resources.Menu;

[Serializable]
public class UserInterface : IResource {
    public int XPosition { get; set; }
    public int YPosition { get; set; }
    public UserInterfaceType UserInterfaceType { get; set; }
    public bool IsModal { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public int ColorSet { get; set; } // 0, 4, 144, 169
    public UiElement[] MenuEntries { get; set; } = [];

    public ResourceType Type {
        get => ResourceType.REQ;
    }

    public string? Title { get; set; }
}