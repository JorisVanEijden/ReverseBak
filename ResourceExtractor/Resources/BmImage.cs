namespace ResourceExtractor.Resources;

using ResourceExtractor.Extractors;

public class BmImage : IResource {
    public ushort Size { get; set; }
    public ushort Flags { get; set; }
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public byte[]? Data { get; set; }
    public ResourceType Type { get => ResourceType.BMX; }
}