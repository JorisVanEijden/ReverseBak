namespace ResourceExtractor.Resources.Image;

public class Screen : IResource {
    public Screen(string filePath) {
        FilePath = filePath;
    }

    public string FilePath { get; }
    public bool HiRes { get; set; }

    public byte[]? BitMapData { get; set; }

    public ResourceType Type {
        get => ResourceType.SCX;
    }
}