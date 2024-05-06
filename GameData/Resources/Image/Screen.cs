namespace GameData.Resources.Image;

public class Screen : IResource {
    public Screen(string id) {
        Id = id;
    }

    public string Id { get; }
    public int Width { get; set; }
    public int Height { get; set; }

    public byte[]? BitMapData { get; set; }

    public ResourceType Type {
        get => ResourceType.SCX;
    }
}