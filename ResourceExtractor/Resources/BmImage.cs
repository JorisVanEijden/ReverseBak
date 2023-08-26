namespace ResourceExtractor.Resources;

public class BmImage {
    public ushort Size { get; set; }
    public ushort Flags { get; set; }
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public byte[]? Data { get; set; }
}