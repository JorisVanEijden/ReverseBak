namespace GameData.Resources.Dialog.Actions;

/// <summary>
/// How a <see cref="SetTimerAction"/> merges with a timer already pending for the same kind and
/// key — <c>timerpool_upsert</c>'s <c>mode</c> (canassa <c>SRC/GAME/STATE/TIMERPL.C</c>).
///
/// <para>Either bit makes the engine look for a matching entry first; with neither, a new one is
/// always appended.</para>
///
/// <para><b>These two were the wrong way round until 2026-08-11.</b> The engine does
/// <c>if (mode &amp; 0x80) p-&gt;nValue += value; else p-&gt;nValue = value;</c> — so 0x80 is the
/// one that accumulates and 0x40 the one that overwrites, the opposite of what the old names said.
/// It mattered: the game's only shipped <c>SetTimer</c> (the corpse-flavour flag 8127) carries
/// 0x40, so reading it as "Add" would have stacked a second two-hour timer every time you looked at
/// a body instead of restarting the one already running. IDA's own <c>timerFlags</c> enum still
/// carries the same inversion — see the comment on <c>timerpool_upsert</c> @0x438F0.</para>
/// </summary>
[Flags]
public enum TimerFlag {
    /// <summary>0x40 — replace the pending timer's remaining time with this one's.</summary>
    Overwrite = 0x40,

    /// <summary>0x80 — add this one's time to whatever is already pending.</summary>
    Accumulate = 0x80,
}
