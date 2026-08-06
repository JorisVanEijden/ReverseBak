namespace GameData.Resources.Dialog.Actions;

/// <summary>
/// Write one of the six dialog text-variable slots — the engine's op 1, dispatched to
/// <c>dialog_cmbt_name_assign_kind(nA1, nA2, nA3, speaker)</c> (DIALOG.C:861).
/// </summary>
public class SetTextVariableAction : DialogActionBase {
    /// <summary>Which slot (0-5) this writes — the <c>@N</c> the text will read back.</summary>
    public int Slot { get; set; }

    /// <summary>The KIND of thing to put there (a specific party member, the chapter speaker, a
    /// constrained random companion, a money amount, …) — not a value. See
    /// <see cref="DialogSlotPopulator.Assign"/>.</summary>
    public int Source { get; set; }

    /// <summary>The kind's third argument (<c>nA3</c>). Only the random picker reads it, as the
    /// one-based slot to copy an actor from when 500 draws fail to satisfy the kind's constraint;
    /// zero means "the party leader". Was named <c>Unknown</c> until the picker was reversed.</summary>
    public int Aux { get; set; }
}
