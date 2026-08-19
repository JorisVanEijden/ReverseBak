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
    /// <summary>
    /// The speaker, as the raw 16-bit field the record holds.
    /// </summary>
    /// <remarks>
    /// <b>It is two values packed into one word.</b> <c>ExecuteDialog</c> splits it before showing
    /// a portrait (@0x49971): the LOW byte is the actor number and the HIGH byte is which frame of
    /// that actor's portrait to draw. Read whole, it is the actor number only while the high byte
    /// is zero.
    ///
    /// <para>Which it is in every shipped file: all 8203 entries across the 32 DDX files have a
    /// high byte of 0, so nothing today needs the split and no speaker is misread. It is documented
    /// because the packing is real and a mod author writing a non-zero frame would find the field
    /// silently naming a different actor.</para>
    /// </remarks>
    public int ActorNumber { get; set; }
    public DialogEntryFlags Flags { get; set; }
    public List<DialogActionBase> Actions { get; set; } = [];
    public List<DialogBranchBase> Branches { get; set; } = [];

    public bool TryGetResizeAction(out ResizeDialogAction? action) {
        action = Actions.OfType<ResizeDialogAction>().FirstOrDefault();

        return action != null;
    }

    /// <summary>
    /// The same entry with different text.
    /// </summary>
    /// <remarks>
    /// <b>For callers that must not write to the entry they were handed.</b> The dialog loader
    /// caches entries, so an entry mutated for one showing comes back changed for the next — which
    /// is how a shop's sign would go missing the second time it is examined, with nothing to say
    /// why. Shallow by design: the actions and branches are shared because nothing rewrites them.
    /// </remarks>
    public DialogEntry WithText(string? text) {
        var copy = (DialogEntry)MemberwiseClone();
        copy.Text = text;

        return copy;
    }
}
