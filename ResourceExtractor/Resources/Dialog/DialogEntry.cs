namespace ResourceExtractor.Resources.Dialog;

public class DialogEntry {
    public int Offset { get; set; }
    public uint Id { get; set; }
    public string Text { get; set; }
    public int DialogEntry_Field0 { get; set; }
    public int DialogEntry_Field1 { get; set; }
    public int DialogEntry_Field3 { get; set; }
    public List<DialogDataItem> DataItems { get; set; } = new();
    public List<DialogEntryVariant> Variants { get; set; } = new();
    public int Referer { get; set; }
}