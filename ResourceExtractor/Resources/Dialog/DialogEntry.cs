namespace ResourceExtractor.Resources.Dialog;

using ResourceExtractor.Resources.Dialog.Actions;

public class DialogEntry {
    public int Offset { get; set; }
    public uint Id { get; set; }
    public string Text { get; set; }
    public int DialogEntry_Field0 { get; set; }
    public int DialogEntry_Field1 { get; set; }
    public int DialogEntry_Field3 { get; set; }
    public List<DialogActionBase> DialogActions { get; set; } = [];
    public List<DialogEntryVariant> Variants { get; set; } = [];
    public int Referer { get; set; }
}