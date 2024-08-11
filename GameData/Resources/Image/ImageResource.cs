namespace GameData.Resources.Image;

public abstract class ImageResource(string id) : IResource {
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[]? BitMapData { get; set; }
    public abstract ResourceType Type { get; }
    public string Id { get; } = id;
}