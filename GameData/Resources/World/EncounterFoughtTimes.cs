namespace GameData.Resources.World;

/// <summary>
/// When each encounter was last fought — <c>GAM_ENC_FOUGHT_TIME</c>, one four-byte game time per
/// encounter — and the recovery that buys its survivors on the way back in.
/// </summary>
/// <remarks>
/// <b>Stamped whatever the fight's outcome</b> (HOTSPOT.C:531 and :790, before the outcome is looked
/// at — see <see cref="EncounterAftermath.FoughtTimeIsStampedRegardless"/>), and read again by
/// <c>combatenc_pty_load_chap_state</c> (CBENC.C:79-84) for every survivor when the encounter is next
/// entered.
/// </remarks>
public sealed class EncounterFoughtTimes {
    /// <summary>One entry per encounter.</summary>
    public const int EntryCount = 700;

    /// <summary>A game time, in two-second ticks.</summary>
    public const int EntrySize = 4;

    /// <summary>Bytes the table occupies.</summary>
    public const int SaveSize = EntryCount * EntrySize;

    /// <summary>Offset of the table <b>within the save body</b> (0x3967).</summary>
    /// <remarks>
    /// GSTATE.H:100-102 lays this table, then the 700-entry visited-time table, immediately before the
    /// encounter object states, so it starts two tables short of
    /// <see cref="EncounterObjectStates.BodyOffset"/>. Checked against shipped saves: the rosters that
    /// precede it and its only stamp (encounter 1, earlier than the save's own clock) both read true.
    /// </remarks>
    public const int BodyOffset = EncounterObjectStates.BodyOffset - (2 * SaveSize);

    /// <summary>Game-time ticks per point of recovery: an hour of two-second ticks.</summary>
    public const int TicksPerRecoveryPoint = 0x708;

    /// <summary>The recovery heals toward the whole combined pool.</summary>
    public const int RecoveryHealTargetPercent = 100;

    private readonly uint[] _times = new uint[EntryCount];

    /// <summary>Read the table out of a save body; a body too short for it reads as never fought.</summary>
    public void Load(byte[] body, int offset = BodyOffset) {
        if (body == null || offset < 0 || offset + SaveSize > body.Length) {
            System.Array.Clear(_times, 0, EntryCount);
            return;
        }
        for (var i = 0; i < EntryCount; i++) {
            _times[i] = System.BitConverter.ToUInt32(body, offset + (i * EntrySize));
        }
    }

    /// <summary>Write the table back into a save body, inverse of <see cref="Load"/>.</summary>
    public bool Save(byte[] body, int offset = BodyOffset) {
        if (body == null || offset < 0 || offset + SaveSize > body.Length) {
            return false;
        }
        for (var i = 0; i < EntryCount; i++) {
            System.BitConverter.GetBytes(_times[i]).CopyTo(body, offset + (i * EntrySize));
        }
        return true;
    }

    /// <summary>Record that an encounter was fought at this game time; out-of-range numbers are ignored.</summary>
    public void Stamp(long encounter, uint gameTime) {
        if (encounter >= 0 && encounter < EntryCount) {
            _times[encounter] = gameTime;
        }
    }

    /// <summary>When an encounter was last fought, or 0 for never.</summary>
    public uint FoughtAt(long encounter) =>
        encounter >= 0 && encounter < EntryCount ? _times[encounter] : 0;

    /// <summary>
    /// The pool delta, in 8.8 fixed point, a survivor gets on the way back in.
    /// </summary>
    /// <remarks>
    /// <c>elapsed = (now - fought) / 0x708</c>, then <c>(long)(elapsed &lt;&lt; 8)</c> — with
    /// <c>elapsed</c> a 16-bit int. <b>The shift happens in sixteen bits, before the widening</b>, so
    /// from 128 hours the value wraps negative and the survivor is DRAINED instead, and at 256 hours it
    /// wraps back to nothing. Reproduced rather than corrected: canassa is byte-matched, so this is
    /// what the player meets. Zero when the encounter has never been fought.
    /// </remarks>
    public static long RecoveryDelta(uint now, uint foughtAt) {
        if (foughtAt == 0) {
            return 0;
        }
        short elapsed = unchecked((short)(((long)now - foughtAt) / TicksPerRecoveryPoint));
        return unchecked((short)(elapsed << 8));
    }
}
