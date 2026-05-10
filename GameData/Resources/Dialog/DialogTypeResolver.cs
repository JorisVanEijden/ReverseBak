namespace GameData.Resources.Dialog;

/// <summary>
/// Faithful port of <c>GetDialogTypeData</c> at 0x4856c in KRONDOR.EXE. Given
/// runtime context and a DDX entry, computes the effective style row index
/// the original game would render — i.e. the value the dispatcher writes
/// back to <c>dialogEntry.dialogType</c> at 0x485af before returning the
/// pointer to the corresponding <c>dialogTypeData</c> row.
/// </summary>
public static class DialogTypeResolver {
    /// <summary>
    /// Compute the effective style row id for this entry under the given
    /// context. Returns a value in 1..6 (the dispatcher never produces 0).
    ///
    /// The disassembly performs the checks in this order; later overrides
    /// win, EXCEPT the in-game flag's <c>jmp</c> at 0x4857c skips the
    /// full-screen check (so full-screen does not stack on top of in-game).
    /// Actor and source-byte checks always run.
    /// </summary>
    public static int ResolveEffectiveStyleId(DialogContext context, byte sourceDialogType, int actorNumber) {
        // Default fallback (mov dx, 2 at 0x4856f).
        int dx = 2;

        // First override gate: in-game context is exclusive with the
        // full-screen flags (the dispatcher's `jmp loc_ovr143_4AF` at 0x4857c
        // bypasses the full-screen branch).
        if (context.InGameContextActive) {
            dx = 5;
        } else if (context.FullScreenFlag1Active || context.FullScreenFlag2Active) {
            dx = 6;
        }

        // Actor override (cmp es:[bx+dialogEntry.actorNr], 0 at 0x48592).
        // Always evaluated; overrides the context-derived value.
        if (actorNumber != 0) {
            dx = 1;
        }

        // Source-byte override (cmp es:[bx+dialogEntry.dialogType], 0 at
        // 0x4859f). Always evaluated; the highest-precedence override.
        if (sourceDialogType != 0) {
            dx = sourceDialogType;
        }

        return dx;
    }

    /// <summary>
    /// Convenience overload that pulls the source byte and actor from a
    /// <see cref="DialogEntry"/>. Does not mutate the entry — the original
    /// game writes the resolved value back to <c>dialogEntry.dialogType</c>,
    /// but the C# port treats the entry as immutable input; callers that
    /// need the dispatcher's write-back semantics should update the entry
    /// themselves with the returned value.
    /// </summary>
    public static int ResolveEffectiveStyleId(DialogContext context, DialogEntry entry) {
        return ResolveEffectiveStyleId(context, (byte)entry.DialogType, entry.ActorNumber);
    }

    /// <summary>
    /// Resolve and look up the style in one step.
    /// </summary>
    public static DialogStyle ResolveStyle(DialogContext context, DialogEntry entry) {
        int rowId = ResolveEffectiveStyleId(context, entry);
        return DialogStyleTable.Get(rowId);
    }
}
