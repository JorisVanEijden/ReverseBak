namespace GameData.Resources.Dialog;

using GameData.Resources.Dialog.Actions;

public class DialogEntry {
    public int Offset { get; set; }
    public uint? Id { get; set; }
    public string? Text { get; set; }
    public DialogType DialogType { get; set; }
    public int ActorNumber { get; set; }
    public DialogEntryFlags Flags { get; set; }
    public List<DialogActionBase> Actions { get; set; } = [];
    public List<DialogEntryBranch> Branches { get; set; } = [];

    public bool TryGetResizeAction(out ResizeDialogAction? action) {
        action = Actions.OfType<ResizeDialogAction>().FirstOrDefault();

        return action != null;
    }
}