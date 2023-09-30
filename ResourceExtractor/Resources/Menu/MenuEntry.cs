namespace ResourceExtractor.Resources.Menu;

using System.Text.Json.Serialization;

public class MenuEntry {
    public ushort Unknown0 { get; set; }
    public ushort Unknown2 { get; set; }
    public bool Unknown4 { get; set; }
    public ushort Unknown5 { get; set; }
    public ushort Unknown7 { get; set; }
    public ushort Unknown9 { get; set; }
    public ushort UnknownB { get; set; }
    public ushort UnknownD { get; set; }
    public ushort UnknownF { get; set; }
    public ushort Unknown11 { get; set; }
    public ushort Unknown13 { get; set; }

    [JsonIgnore]
    public short LabelOffset { get; init; }

    public ushort Unknown17 { get; set; }
    public ushort Unknown19 { get; set; }
    public ushort Unknown1B { get; set; }
    public ushort Unknown1D { get; set; }
    public ushort Unknown1F { get; set; }
    public string Label { get; set; }
}