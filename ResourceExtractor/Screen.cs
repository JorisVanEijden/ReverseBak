namespace ResourceExtractor;

internal class Screen {
    public Screen(string filePath) {
        FilePath = filePath;
    }

    public string FilePath { get; }
    public int Width { get; set; }
    public int Height { get; set; }

    public byte[]? BitMapData { get; set; }
}