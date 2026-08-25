namespace GameData.Resources.World;

using System;
using System.Collections.Generic;

/// <summary>
/// The per-encounter hook that runs when a fight is WON — <c>evtcond_dispatch_key_to_handler</c>
/// (canassa EVTCOND.C:406).
///
/// <para>A flat switch over encounter numbers with four arms, which are three different mechanics
/// rather than one: a line of dialog, a pair of "clear the whole group" gates, and eleven
/// encounters that put themselves back. Nothing here is derivable from the encounter data — the
/// numbers are compiled into the executable, so this table IS the rule.</para>
/// </summary>
/// <remarks>
/// <b>It runs after the fought flag is written, and the group gates depend on that.</b> The original
/// calls <c>hotspotevt_enc_fought_set</c> immediately before dispatching here, and the gates then
/// read the fought flags of every member INCLUDING the one just defeated. Calling this first makes
/// the last kill of a group look like the second-to-last, and the group flag is never earned at all.
/// </remarks>
public static class EncounterCompletion {
    /// <summary>Nothing to play / no flag to set.</summary>
    public const int None = 0;

    // ---------------------------------------------------------------- the one that speaks

    /// <summary>The encounter that plays a line when it is beaten.</summary>
    public const long SpeakingEncounter = 0x4a;

    /// <summary>The dialog record <see cref="SpeakingEncounter"/> plays.</summary>
    public const uint SpeakingEncounterDialog = 0x1cfdf1;

    /// <summary>
    /// The dialog record a defeated encounter plays, or <see cref="None"/>.
    /// </summary>
    /// <remarks>
    /// <b>Not modal.</b> The original passes 0 where every other hotspot dialog passes 1, so this
    /// one plays without waiting for the player — it is a parting line, not a prompt.
    /// </remarks>
    public static uint DialogAfterDefeat(long encounter) =>
        encounter == SpeakingEncounter ? SpeakingEncounterDialog : None;

    /// <inheritdoc cref="DialogAfterDefeat"/>
    public static bool DialogWaitsForThePlayer => false;

    // ---------------------------------------------------------------- the ones that come back

    /// <summary>
    /// Encounters that RE-ARM when they are beaten, through the full reset
    /// (<see cref="EncounterReset"/>): fought, done, scout-tried and scouted all cleared, and the
    /// creature group reloaded.
    /// </summary>
    /// <remarks>
    /// <b>The reload is not optional.</b> Defeating an encounter kills every actor on its roster, so
    /// clearing the flags without reloading the group leaves an encounter that arms again and then
    /// fields nobody — which reads as a broken record rather than as a respawn.
    /// </remarks>
    public static IReadOnlyList<long> ReArmingEncounters { get; } = new long[] {
        0xeb, 0xf5, 0x123, 0x125, 0x14f, 0x151, 0x152, 0x177, 0x19a, 0x1ad, 0x1ae,
    };

    /// <inheritdoc cref="ReArmingEncounters"/>
    public static bool ReArmsWhenDefeated(long encounter) {
        for (var i = 0; i < ReArmingEncounters.Count; i++) {
            if (ReArmingEncounters[i] == encounter) {
                return true;
            }
        }
        return false;
    }

    // ---------------------------------------------------------------- the ones that count

    /// <summary>
    /// A set of encounters that earns one flag once EVERY member has been beaten.
    /// </summary>
    public readonly struct Group {
        internal Group(int flag, long[] members) {
            Flag = flag;
            Members = members;
        }

        /// <summary>The global event key set when the group is complete.</summary>
        public int Flag { get; }

        /// <summary>Every encounter that has to be beaten first.</summary>
        public IReadOnlyList<long> Members { get; }
    }

    /// <summary>
    /// The two counted groups.
    /// </summary>
    /// <remarks>
    /// <b>The gate is checked on EVERY member's defeat, not only the last one</b>, because nothing
    /// knows which is last — the party can beat them in any order, so each defeat asks whether it
    /// completed the set.
    /// </remarks>
    public static IReadOnlyList<Group> Groups { get; } = new[] {
        new Group(0xdb1c, new long[] { 0x83, 0x84, 0x85, 0x86, 0x87 }),
        new Group(0x1d17, new long[] { 0x262, 0x265, 0x267, 0x26a, 0x26b, 0x26d }),
    };

    /// <summary>
    /// The flag a defeat completes, or <see cref="None"/>.
    /// </summary>
    /// <param name="encounter">The encounter just beaten.</param>
    /// <param name="isFought">Whether a given encounter's fought flag is set.</param>
    /// <remarks>
    /// <b>The member ids and the flags they gate are the same numbers.</b> The original writes the
    /// gate as five or six literal <c>gstate_event_read</c> calls, and every one of those keys is
    /// exactly <c>5220 + member</c> — the encounter's own fought flag. Reading the two lists as
    /// unrelated is what makes this look like an opaque set of magic constants.
    ///
    /// <para><b>Only the group the encounter belongs to is asked.</b> A defeat outside both groups
    /// completes nothing, and a group is never completed by a defeat that is not one of its
    /// members.</para>
    /// </remarks>
    public static int GroupFlagEarnedBy(long encounter, Func<long, bool> isFought) {
        if (isFought == null) {
            return None;
        }

        for (var g = 0; g < Groups.Count; g++) {
            Group group = Groups[g];
            if (!Contains(group.Members, encounter)) {
                continue;
            }
            for (var i = 0; i < group.Members.Count; i++) {
                if (!isFought(group.Members[i])) {
                    return None;
                }
            }
            return group.Flag;
        }
        return None;
    }

    /// <summary>Whether anything at all happens when this encounter is beaten.</summary>
    public static bool HasFollowup(long encounter) =>
        DialogAfterDefeat(encounter) != None
        || ReArmsWhenDefeated(encounter)
        || GroupOf(encounter) >= 0;

    /// <summary>Index into <see cref="Groups"/>, or -1.</summary>
    public static int GroupOf(long encounter) {
        for (var g = 0; g < Groups.Count; g++) {
            if (Contains(Groups[g].Members, encounter)) {
                return g;
            }
        }
        return -1;
    }

    private static bool Contains(IReadOnlyList<long> members, long encounter) {
        for (var i = 0; i < members.Count; i++) {
            if (members[i] == encounter) {
                return true;
            }
        }
        return false;
    }
}
