namespace GameData.Resources.Combat;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The line a fight ends on — the tail of <c>combat_arena_actor_turn_loop</c> (COMBAT.C:2587-2672).
/// </summary>
/// <remarks>
/// <para>Every fight the party does not run from ends with one of these, chosen by who is left
/// lying on the field: a knocked-down party member (0x83), the number of enemy bodies (0x20 / 0x81,
/// or 0x142 for a handful of set-piece encounters), "the enemy had fled" when there are none
/// (0x82), or the trap-puzzle exit line (0x1f). Makala (encounter 0x221) has his own (0x150), and
/// a wiped party gets 0x21 (0x6b on a trap puzzle).</para>
///
/// <para>"Down" is the original's <c>combatenc_count_active_enemies</c>: dead and not fled. It is
/// asked of the party first, through <c>combat_arena_swap_tgt_state</c>, then of the enemies.</para>
/// </remarks>
public static class CombatOutcomeDialog {
    /// <summary>Makala's fight — <c>g_encounter_id == 0x221</c>.</summary>
    public const int MakalaEncounter = 0x221;

    /// <summary>A party member fell and got up again.</summary>
    public const int PartyMemberDown = 0x83;

    /// <summary><c>hotspotevt_monst_dispatch_by_tag</c> (HOTSPOT.C:1121): encounters whose
    /// many-bodies line is 0x142 rather than 0x20.</summary>
    public static readonly IReadOnlyCollection<int> SetPieceEncounters = new HashSet<int> {
        0x97, 0x98, 0xeb, 0xf5, 0x123, 0x125, 0x14f, 0x151, 0x152, 0x177, 0x19a, 0x1ad, 0x1ae,
    };

    /// <summary><c>combat_arena_all_targets_immune</c> (COMBAT.C:304): creatures that vanish
    /// rather than die.</summary>
    public static bool Vanishes(int classId) => classId is 0x31 or 0x38 or 0x39;

    /// <summary>
    /// The dialog record the fight ends on, or null when it ends in silence (a flight).
    /// </summary>
    public static int? For(CombatEncounter fight, int encounter, bool trapPuzzle, bool fled = false) {
        if (fight.PartyAlive() == 0) {
            return trapPuzzle ? 0x6b : 0x21;
        }
        if (fled) {
            return null;
        }
        if (encounter == MakalaEncounter) {
            return 0x150;
        }
        if (fight.Party.Any(c => c.IsPartyMember && IsDown(c))) {
            return PartyMemberDown;
        }
        bool allVanish = fight.Enemies.All(c => Vanishes(c.ClassId));
        int bodies = fight.Enemies.Count(IsDown);
        if (bodies > 1) {
            return allVanish ? 0x131 : SetPieceEncounters.Contains(encounter) ? 0x142 : 0x20;
        }
        if (bodies == 1) {
            return allVanish ? 0x132 : 0x81;
        }
        return trapPuzzle ? 0x1f : 0x82;
    }

    /// <summary>The downed party member the 0x83 line names (<c>nEvtArgActor0</c>): the last one
    /// in roster order, as the original's loop leaves it.</summary>
    public static Combatant DownedMember(CombatEncounter fight) =>
        fight.Party.LastOrDefault(c => c.IsPartyMember && IsDown(c));

    /// <summary>The first member still standing (<c>nEvtArgActor1</c> for the 0x83 line).</summary>
    public static Combatant StandingMember(CombatEncounter fight) =>
        fight.Party.FirstOrDefault(c => c.IsPartyMember && !c.IsDead);

    private static bool IsDown(Combatant c) =>
        c.IsDead && (c.Flags & CombatantFlags.Fleeing) == 0;
}
