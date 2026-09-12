namespace GameData.Resources.World;

/// <summary>
/// Whether the party may make camp where it stands — the gate at the top of <c>encamp_run</c>
/// (canassa <c>SRC/SCREENS/ENCAMP.C:78-80</c>).
/// </summary>
/// <remarks>
/// <b>The camp screen does not open at all when something is watching.</b> The original's first act
/// is <c>if (proxscan_vis_rec_kind_0e3e()) dialog_play_record(0x65, 1);</c> — it plays "Something
/// moved. <i>On second thought, let's not sleep here</i>" and returns, so there is no dial, no rest
/// and no time passed. A port that opens the panel regardless lets the party bed down beside a
/// monster.
///
/// <para><b>Measured 2026-09-12, and the consequence is not cosmetic.</b> Same save
/// (<c>dir.G01/SAVE13</c>, the party already short of rations) camped in both games: the original
/// refused with dialog 101, ours rested and came out at 7/55, 3/40 and 27/60 health with zero
/// stamina across the party and no food left. The refusal is what stops that.</para>
///
/// <para><b>What counts as watching:</b> <c>proxscan_vis_rec_kind_0e3e</c> rebuilds the visible-entry
/// list and returns 1 for the first entry that is an encounter actor
/// (<c>shapeId == g_nProximityTableCount</c>) whose state kind is 3 —
/// <c>rgnenc_slot_actor_kind_eq_placed</c>, which is <see cref="EncounterObjectStates.KindRoaming"/>.
/// A standing guard or a corpse does not stop you sleeping; something on the move does.</para>
///
/// <para><b>Underground the sight range is capped and above ground it is not.</b> PROXSCAN.C:264 is
/// <c>if (g_game_mode != 2 || *plDist &lt;= 0x2134)</c>, so in a dungeon only an actor within
/// <see cref="UndergroundSightRange"/> counts, while in the open anything the scan listed does.</para>
/// </remarks>
public static class CampRefusal {
    /// <summary>The DDX record played instead of opening the camp screen — <c>0x65</c>.</summary>
    public const uint WatchedDialogId = 0x65;

    /// <summary>How far a dungeon's scan sees an encounter actor — <c>0x2134</c>.</summary>
    public const int UndergroundSightRange = 0x2134;

    /// <summary>
    /// Whether one placed encounter actor is close enough, and active enough, to forbid camping.
    /// </summary>
    /// <param name="roams">Whether its state kind is Roaming — the original's <c>state == 3</c>.</param>
    /// <param name="octagonalDistance">Its distance from the party, in the scan's own metric.</param>
    /// <param name="underground">Whether the zone is a dungeon (<c>g_game_mode == 2</c>).</param>
    public static bool Watches(bool roams, long octagonalDistance, bool underground) =>
        roams && (!underground || octagonalDistance <= UndergroundSightRange);
}
