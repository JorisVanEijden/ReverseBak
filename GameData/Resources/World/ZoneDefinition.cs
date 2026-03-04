namespace GameData.Resources.World;

public class ZoneDefinition : IResource
{
    public ZoneDefinition(string id) { Id = id; }
    public ushort ZoneLocation { get; set; }
    public ushort ZonePointer { get; set; }
    public uint Field04 { get; set; }
    public ushort Field08 { get; set; }
    public ushort Flags { get; set; }
    public byte Unknown0C { get; set; }
    public byte Unknown0D { get; set; }
    public uint Field0E { get; set; }
    public uint CameraZPosition { get; set; }
    public uint Field16 { get; set; }
    public uint Field1A { get; set; }
    public ushort RmpResourceCount { get; set; }
    public ushort Field20 { get; set; }
    public uint Field22 { get; set; }
    public uint Field26 { get; set; }
    public ushort Field2A { get; set; }
    public uint Field2C { get; set; }
    public uint Field30 { get; set; }
    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }
}
