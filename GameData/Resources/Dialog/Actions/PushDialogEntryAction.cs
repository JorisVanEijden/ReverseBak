namespace GameData.Resources.Dialog.Actions;

/// <summary>
/// Action type 16. Queues a continuation dialog target onto a 5-slot LIFO
/// (<c>dialogEntryQueue</c> in <c>ExecuteDialog</c>) so that when the current
/// dialog tree finishes, control resumes at <see cref="Offset"/>. The push
/// only fires when a branch was already chosen by branch evaluation
/// (<c>dialogIdOrOffset != 0</c>); when the dialog ends and the queue is
/// non-empty, the engine pops one and re-enters with that target.
/// <para>
/// <see cref="Offset"/> follows the same 32-bit encoding as a branch target:
/// bit 31 set → entry-Id reference; cleared → raw file offset.
/// </para>
/// </summary>
public class PushDialogEntryAction : DialogActionBase {
    public int Offset { get; set; }
    public int Field4 { get; set; }
    public int Field6 { get; set; }
}
