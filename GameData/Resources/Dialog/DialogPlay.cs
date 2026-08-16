namespace GameData.Resources.Dialog;

/// <summary>
/// One play of a dialog: the entry that ended up displayable, plus the six text-variable slots as
/// they stood when the walk reached it.
///
/// <para>The two travel together because the slots are the state of <b>this</b> play, not of the
/// entry — the engine re-seeds them per <c>dialog_play_record</c> (DIALOG.C:849), so showing the
/// same entry twice can legitimately name different companions. Handing the entry around without
/// its slots is what loses the writes made by the text-less router entries the walk passed
/// through.</para>
/// </summary>
public sealed class DialogPlay {
    public DialogPlay(DialogEntry entry, DialogSlotTable slots, DialogSlotContext context,
        Dialog dialog = null) {
        Entry = entry;
        Slots = slots;
        Context = context;
        Dialog = dialog;
    }

    /// <summary>The leaf the branch walk stopped on — the entry whose text is shown.</summary>
    public DialogEntry Entry { get; }

    /// <summary>The slots this play accumulated: seeded, then written by every entry the walk
    /// touched, in order.</summary>
    public DialogSlotTable Slots { get; }

    /// <summary>The state the slots were filled from. Carried so the renderer can resolve a bare
    /// <c>@</c> to the same actor the slots were built against.</summary>
    public DialogSlotContext Context { get; }

    /// <summary>
    /// The dialog <see cref="Entry"/> came from, so a conversation can find its next line.
    /// </summary>
    /// <remarks>
    /// Carried rather than re-loaded: continuing a conversation asks the SAME dialog for the entry
    /// after this one, and reloading it per line would re-seed the slots this play has been
    /// accumulating. Null for plays built before the chain existed, which simply cannot continue.
    /// </remarks>
    public Dialog Dialog { get; }
}
