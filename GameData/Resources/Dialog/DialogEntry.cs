namespace GameData.Resources.Dialog;

using GameData.Resources.Dialog.Actions;
using GameData.Resources.Dialog.Branches;

public class DialogEntry {
    public int Offset { get; set; }

    /// <summary>Stable content-graph key of this dialog entry: <c>base:ddx:&lt;file&gt;:&lt;offset&gt;</c>
    /// (e.g. <c>base:ddx:dial_z01:1234</c>). Offset-derived and per-DDX (offsets are unique within a
    /// file). Branch/push offset-references resolve to this key; entries that also have a global
    /// <see cref="Id"/> are additionally addressable as <c>base:dialog:&lt;Id&gt;</c>. See
    /// docs/re-notes/reference-inventory.md #3/#4.</summary>
    public string Key { get; set; } = "";

    public uint? Id { get; set; }
    public string? Text { get; set; }
    public DialogType DialogType { get; set; }
    public int ActorNumber { get; set; }
    public DialogEntryFlags Flags { get; set; }
    public List<DialogActionBase> Actions { get; set; } = [];
    public List<DialogBranchBase> Branches { get; set; } = [];

    public bool TryGetResizeAction(out ResizeDialogAction? action) {
        action = Actions.OfType<ResizeDialogAction>().FirstOrDefault();

        return action != null;
    }
}