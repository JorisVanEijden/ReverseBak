namespace GameData.Resources.Image;

public class BmImage(string id) : ImageResource(id) {
    public int Size { get; set; }
    public ushort Flags { get; set; }
    public override ResourceType Type { get => ResourceType.BMX; }
}