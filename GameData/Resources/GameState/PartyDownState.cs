namespace GameData.Resources.GameState;

using System.Collections.Generic;

/// <summary>
/// What the party-down byte MEANS — <c>g_gameState.bCombatExitRequest</c>, which our save model
/// calls <c>PartyDeathState</c> (body offset 14).
/// </summary>
/// <remarks>
/// <b>The byte is written in three places and read in seven, and we had only the writes.</b> A pit
/// fall, the arena's last kill and the stat sweep all set it; nothing on our side ever looked at it,
/// so a party wipe set a flag and the game carried on exactly as before.
///
/// <para><b>The name is the usual trap.</b> "Combat exit request" describes one of its writers, not
/// the byte — it is read by the world loop, the map screen, the encampment screen, the in-combat
/// inventory screen and the dialog event sweep, none of which are combat exiting. Its neighbour at
/// offset 15 (<c>nWorldLoopExitRequest</c>, ours <c>ChapterTransitionPending</c>) is the one that
/// really is a loop-exit request, which is how the two got confused.</para>
/// </remarks>
/// <remarks>
/// <b>PARTLY CONSUMED (TASK-264).</b> The world loop now ends on it — <c>InGameScreen.Update</c>
/// runs <c>post_dispatch</c> and leaves for the main menu, and <c>HotspotService</c> gates its
/// activate pass behind <see cref="HotspotsStillFire"/>, so a downed party springs nothing while
/// still falling into a pit it walks onto.
///
/// <para>Two rules here have no reader and <b>no reachable trigger</b> (TASK-264 closed on this):
/// <see cref="ConditionEventsSweep"/> guards a condition/skill notice sweep this port has never
/// built, and the map screen's copy of the exit cannot fire because the navigator hides the world
/// screen while the map is up and nothing can set the byte from there. Both become real work the
/// day their surface exists; neither is a missing guard today.</para>
/// </remarks>
public static class PartyDownState {
    /// <summary>The party is up. Everything runs normally.</summary>
    public const int Standing = 0;

    /// <summary>
    /// <b>Noticed</b> — the stat sweep found every active member afflicted, and says so.
    /// </summary>
    /// <remarks>
    /// This is the only value that speaks: both loops play dialog 0x145 as they stop.
    /// </remarks>
    public const int Noticed = 1;

    /// <summary>
    /// <b>Asserted</b> — something put the party down and has already said its piece.
    /// </summary>
    /// <remarks>
    /// The pit fall (<c>WORLDCRS.C:82</c>) writes this rather than 1, having already shown its
    /// landing dialog.
    ///
    /// <para><b>The arena's <c>CACTOR.C:2099</c> is NOT an ordinary defeat.</b> It sits in
    /// <c>combat_actor_kill_remaining_enc</c>, reached only from COMBAT.C case 16 behind
    /// <c>key_is_down(0x1d)</c> and a confirmation dialog — a give-up command. A party wiped by
    /// damage goes through <see cref="Recompute"/> and yields <see cref="Noticed"/> instead, because
    /// <c>combat_arena_actor_die</c> applies Near-death and the sweep re-derives from that.</para>
    /// </remarks>
    public const int Asserted = 2;

    /// <summary>The dialog the loops play on their way out, and only for <see cref="Noticed"/>.</summary>
    public const int NoticedDialogId = 0x145;

    /// <summary>
    /// Whether the world loop and the map screen should stop.
    /// </summary>
    /// <remarks>
    /// <b>Any non-zero value, not just 1.</b> <c>WORLDLP.C:399</c> and <c>MAP.C:447</c> both test
    /// <c>!= 0</c>; only the dialog distinguishes the two values.
    /// </remarks>
    public static bool EndsTheLoop(int state) => state != Standing;

    /// <summary>
    /// Whether dialog <see cref="NoticedDialogId"/> plays as the loop ends.
    /// </summary>
    /// <remarks>
    /// <b>Exactly 1, not "non-zero".</b> <c>WORLDLP.C:413</c> and <c>MAP.C:455</c> both test
    /// <c>== 1</c>. Widening it to non-zero makes the pit fall play its landing dialog and then this
    /// one, which is the double message the 1/2 split exists to avoid.
    /// </remarks>
    public static bool PlaysTheNoticedDialog(int state) => state == Noticed;

    /// <summary>
    /// Whether hotspots still fire underfoot.
    /// </summary>
    /// <remarks>
    /// <b>They stop, but the PIT does not.</b> <c>WORLDLP.C</c> calls
    /// <c>worldcross_dungeon_descent_anim</c> <i>before</i> the guard and gates
    /// <c>hotspotevt_activate_at_player</c> behind it, so a downed party still falls into a pit it
    /// walks onto while every other trigger goes quiet. Guarding the whole block would change that.
    /// </remarks>
    public static bool HotspotsStillFire(int state) => state == Standing;

    /// <summary>
    /// Whether the dialog layer's condition/skill event sweep runs.
    /// </summary>
    /// <remarks>
    /// <c>EVTCOND.C:332</c> returns immediately when the byte is set — so a downed party raises no
    /// "skill improved" or condition notices. Worth having: a port that keeps sweeping would pop
    /// advancement dialogs over a party that has just been wiped out.
    /// </remarks>
    public static bool ConditionEventsSweep(int state) => state == Standing;

    /// <summary>
    /// The value the stat sweep computes — <c>STAT.C:383</c>.
    /// </summary>
    /// <param name="nearDeathRanks">Each ACTIVE member's Near-death rank (condition 6).</param>
    /// <remarks>
    /// <b>It is a RECOMPUTE, not a latch, and the test is "non-zero" rather than "full".</b> The
    /// loop starts at 1 and clears to 0 on the first active member whose Near-death rank is zero, so
    /// a party where everyone is <i>slightly</i> near death reads as down, and healing one member
    /// back to zero clears it again.
    ///
    /// <para>Two consequences worth stating. A port that requires <c>MaxRank</c> would never fire
    /// this from ordinary damage. And because it overwrites, a later sweep can turn an
    /// <see cref="Asserted"/> 2 into a 1 or a 0 — the pit's "already spoke" marker does not
    /// survive a heal, and nothing tries to make it.</para>
    /// </remarks>
    public static int Recompute(IReadOnlyList<int> nearDeathRanks) {
        if (nearDeathRanks == null) {
            return Standing;
        }
        foreach (int rank in nearDeathRanks) {
            if (rank == 0) {
                return Standing;
            }
        }
        return Noticed;
    }

    /// <summary>
    /// <b>An empty active party reads as DOWN.</b>
    /// </summary>
    /// <remarks>
    /// The original's loop runs <c>partySize</c> times from an initial 1, so zero members leaves the
    /// 1 standing. Not obviously intentional, and reproduced rather than special-cased — it is only
    /// reachable in a state the game does not otherwise allow.
    /// </remarks>
    public static bool AnEmptyPartyReadsAsDown => Recompute(new int[0]) == Noticed;
}
