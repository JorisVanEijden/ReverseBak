namespace GameData.Resources.Dialog;

/// <summary>
/// The six text-variable slots a dialog's <c>@N</c> tokens read — the engine's
/// <c>g_speaker_names[6][32]</c> / <c>g_speaker_kinds[6]</c> pair (DIALOG.C:69,
/// <c>six32byteStrings</c> @0x3ebb4 / <c>dialogActorSlots</c> @0x3ec74).
///
/// <para><b>Lifetime is one dialog play, not one entry.</b> <c>dialog_play_record</c> (DIALOG.C:849)
/// resets and seeds the table once, before it starts walking records; each record it then displays
/// may overwrite individual slots through its own ops. A table rebuilt per entry loses the seeded
/// defaults, which is what left <c>@4</c> unresolved.</para>
/// </summary>
public sealed class DialogSlotTable {
    public const int SlotCount = 6;

    /// <summary>Kind value meaning "this slot is not a party member" — the engine's 0xFF, written
    /// at the top of every assignment before the kind switch decides otherwise.</summary>
    public const int NoActor = 0xFF;

    /// <summary>Kind value meaning "this slot holds a creature name, not a party member" (0xFE).
    /// The renderer treats those differently (the a/an article, the possessive), which is why the
    /// kind is tracked alongside the name rather than thrown away.</summary>
    public const int CreatureActor = 0xFE;

    /// <summary>The substituted text per slot. Empty means "nothing was ever put here" — the
    /// engine renders that as nothing at all, never as the literal token.</summary>
    public string[] Names { get; } = { "", "", "", "", "", "" };

    /// <summary>Which actor each slot currently holds: a party-member id, or
    /// <see cref="NoActor"/> / <see cref="CreatureActor"/>.</summary>
    public int[] Kinds { get; } = { NoActor, NoActor, NoActor, NoActor, NoActor, NoActor };

    /// <summary>Clear every slot — <c>dialog_combatant_name_table_init</c>'s first loop
    /// (DIALOG.C:783-787). The seeding that follows it lives in
    /// <see cref="DialogSlotPopulator.CreateForPlay"/>.</summary>
    public void Clear() {
        for (int i = 0; i < SlotCount; i++) {
            Names[i] = "";
            Kinds[i] = NoActor;
        }
    }

    /// <summary>True when <paramref name="actorId"/> already occupies a slot BELOW
    /// <paramref name="slot"/>. The random picker refuses such a candidate, which is what stops two
    /// slots in one sentence naming the same party member — and it deliberately looks only at lower
    /// slots, so the seeding ORDER (4, 5, 3, 0) decides who defers to whom.</summary>
    public bool IsTakenBelow(int slot, int actorId) {
        for (int i = 0; i < slot && i < SlotCount; i++) {
            if (Kinds[i] == actorId) {
                return true;
            }
        }
        return false;
    }

    /// <summary>Returned by <see cref="ResolveActorOperand"/> for the party-wide form.</summary>
    public const int PartyWide = -1;

    /// <summary>Returned by <see cref="ResolveActorOperand"/> when the slot holds no party member.</summary>
    public const int Unresolved = -2;

    /// <summary>First operand value that names a speaker slot; 0 and 1 mean the whole party.</summary>
    public const int FirstSpeakerOperand = 2;

    /// <summary>
    /// Turns a dialog action's actor operand into a party member id.
    /// </summary>
    /// <remarks>
    /// <para>The operand is <b>not</b> a member id. The original writes it as
    /// <c>g_speaker_kinds[operand - 2]</c>, and guards that with <c>operand &gt; 1</c> — so 0 and 1
    /// are reserved to mean "the whole active party" and everything above indexes this table, biased
    /// by two. Reading the operand directly as a member id would act on the wrong character, and
    /// would do it silently.</para>
    /// <para>A slot holding a creature rather than a party member, or never filled, resolves to
    /// <see cref="Unresolved"/> — the caller must not fall back to "somebody".</para>
    /// </remarks>
    public int ResolveActorOperand(int operand) {
        if (operand <= 1) {
            return PartyWide;
        }
        int slot = operand - FirstSpeakerOperand;
        if (slot < 0 || slot >= SlotCount) {
            return Unresolved;
        }
        int kind = Kinds[slot];
        return kind == NoActor || kind == CreatureActor ? Unresolved : kind;
    }
}
