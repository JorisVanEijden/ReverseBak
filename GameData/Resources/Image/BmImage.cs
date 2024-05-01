namespace GameData.Resources.Image;

public class BmImage : IResource {
    public int Size { get; set; }
    public ushort Flags { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[]? Data { get; set; }
    public ResourceType Type { get => ResourceType.BMX; }
}