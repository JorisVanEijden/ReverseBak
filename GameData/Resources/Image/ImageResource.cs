namespace GameData.Resources.Image;

#if JSON_SERIALIZE
using System.Text.Json.Serialization;
#endif

public abstract class ImageResource(string id) : IResource {
    public int Width { get; set; }
    public int Height { get; set; }
    public virtual double ScaleX { get; set; }
    public virtual double ScaleY { get; set; }
    public abstract ResourceType Type { get; }

    public string Id { get; } = id;

    public string? Filename { get; set; }

#if JSON_SERIALIZE
[JsonIgnore]
#endif
    public byte[]? BitMapData { get; set; }
}