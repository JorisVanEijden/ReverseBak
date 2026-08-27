namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// The six AI turn entry points and the target filter each one asks for —
/// <c>combat_ai_turn_kind6</c> and its five <c>combat_ai_turn_packet_*</c> siblings
/// (canassa CBTAI.C:259-300).
///
/// <para>Each is a one-line wrapper around <c>combat_ai_execute_turn(actor, 6, N)</c>. That makes
/// two things concrete which were previously guesses.</para>
/// </summary>
public static class AiTurnPackets {
    /// <summary>
    /// <b>THIS WRAPPER FAMILY's target search radius — not the AI's.</b>
    /// </summary>
    /// <remarks>
    /// Every one of the six <c>combat_ai_execute_turn</c> wrappers passes 6 as
    /// <c>combatenc_ai_pick_target_by_role</c>'s <c>max_distance</c>, and this is that value.
    ///
    /// <para><b>It was documented here as "the AI's target search radius is a constant 6", which is
    /// true of these six wrappers and of nothing else.</b> Two other families were named later and
    /// pass different numbers — melee sweeps 100, a crossbow shot sweeps 10, and only the "anyone"
    /// variants fall back to 6. Use <see cref="CombatAi.SearchRadiusFor"/>, which knows all three;
    /// reading this constant as the AI's radius is what put a resolver-wide 12 into the game.</para>
    ///
    /// <para><b>It is not the grid's reach.</b> The arena is 8 x 13, so a monster simply cannot see a
    /// target on the far side — a port that passed the grid diagonal instead would have monsters
    /// engaging across the whole field.</para>
    /// </remarks>
    public const int TargetSearchRadius = 6;

    /// <summary>
    /// Which <see cref="TargetRole"/> each wrapper selects, in wrapper order.
    /// </summary>
    /// <remarks>
    /// The wrappers pass filter values 0 through 5, which map straight onto our enum — it was
    /// numbered from the same source. <b><see cref="TargetRole.Disengaged"/> (6) is reached by no
    /// wrapper at all</b>, so nothing in the AI's ordinary turn asks for a target whose own quarry
    /// just died; that filter must be reached some other way, if at all.
    /// </remarks>
    public static readonly IReadOnlyList<TargetRole> RoleByPacket = new[] {
        TargetRole.Anyone,               // combat_ai_turn_kind6          -> 0
        TargetRole.Spellcaster,          // combat_ai_turn_packet_10006   -> 1
        TargetRole.Wounded,              // combat_ai_turn_packet_20006   -> 2
        TargetRole.MissileCapable,       // combat_ai_turn_packet_30006   -> 3
        TargetRole.Engaged,              // combat_ai_turn_packet_40006   -> 4
        TargetRole.TargetingTheLeader,   // combat_ai_turn_packet_50006   -> 5
    };

    /// <summary>How many distinct turn packets exist.</summary>
    public const int PacketCount = 6;

    /// <summary>The role a packet index selects.</summary>
    public static TargetRole RoleFor(int packet) =>
        packet >= 0 && packet < RoleByPacket.Count ? RoleByPacket[packet] : TargetRole.Anyone;

    /// <summary>
    /// Whether a role is reachable through the ordinary turn packets.
    /// </summary>
    public static bool IsReachableFromAPacket(TargetRole role) {
        for (var i = 0; i < RoleByPacket.Count; i++) {
            if (RoleByPacket[i] == role) {
                return true;
            }
        }
        return false;
    }
}
