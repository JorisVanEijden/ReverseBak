namespace GameData.Resources.Dialog;

using GameData.Resources.Dialog.Actions;

public class DialogEntry {
    public int Offset { get; set; }
    public uint Id { get; set; }
    public string Text { get; set; }
    public int DialogEntry_Field0 { get; set; }
    public int DialogEntry_Field1 { get; set; }
    public DialogEntryFlags Flags { get; set; }
    public List<DialogActionBase> Actions { get; set; } = [];
    public List<DialogEntryBranch> Branches { get; set; } = [];
    public int Referer { get; set; }
}