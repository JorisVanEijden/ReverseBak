namespace ResourceExtractor.Resources;

public class MenuData {
    public int XPosition { get; set; }
    public int YPosition { get; set; }
    public MenuType Type { get; set; }
    public bool Modal { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public ushort Unknown1 { get; set; }
    public ushort Unknown2 { get; set; }
    public ushort Unknown3 { get; set; }
    public uint Unknown4 { get; set; }
    public ushort Unknown0 { get; set; }
    public MenuEntry[] MenuEntries { get; set; } = Array.Empty<MenuEntry>();
}

public enum MenuType {
    Unknown0 = 0x0000,
    Unknown1 = 0x0001,
    Unknown2 = 0x0002,
}