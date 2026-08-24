namespace GameData.Resources.World;

using System;
using System.Collections.Generic;

/// <summary>
/// Marking an encounter defeated — <c>rgnenc_mark_defended</c> (canassa RGNENC.C:478).
///
/// <para>It does three separable things, and a port that does only the obvious one leaves the
/// encounter re-fightable:</para>
/// <list type="number">
///   <item>flags the encounter as FOUGHT;</item>
///   <item><b>stops every roaming actor</b> in the record — see
///     <see cref="EncounterObjectStates.StopRoaming"/>;</item>
///   <item><b>kills every still-living actor on its roster</b>, whether or not the party ever
///     touched it.</item>
/// </list>
/// </summary>
public static class EncounterDefeat {
    /// <summary>Roster slots per encounter record; <c>-1</c> marks an empty one.</summary>
    public const int RosterSlots = EncounterObjectStates.SlotsPerRecord;

    /// <summary>An empty roster slot.</summary>
    public const int EmptyRosterSlot = -1;

    /// <summary>
    /// The filter value meaning "every encounter record", not "record 0".
    /// </summary>
    /// <remarks>
    /// The original takes an encounter id and skips a record when
    /// <c>filter != 0 &amp;&amp; filter != enc_id</c>, so <b>zero disables the filter entirely</b>
    /// rather than selecting the first record. Reading it as an ordinary id would defeat exactly one
    /// encounter where the game defeats them all.
    /// </remarks>
    public const long AllEncounters = 0;

    /// <summary>Whether a record is covered by a given filter.</summary>
    public static bool Matches(long filterEncounterId, long recordEncounterId) =>
        filterEncounterId == AllEncounters || filterEncounterId == recordEncounterId;

    /// <summary>What one call changed, for a caller that has to persist it.</summary>
    public readonly struct Result {
        public Result(int recordsMarked, int actorsStopped, IReadOnlyList<int> actorsKilled) {
            RecordsMarked = recordsMarked;
            ActorsStopped = actorsStopped;
            ActorsKilled = actorsKilled;
        }

        /// <summary>Encounter records the filter matched.</summary>
        public int RecordsMarked { get; }

        /// <summary>Object-state slots taken off patrol.</summary>
        public int ActorsStopped { get; }

        /// <summary>Roster actor ids that were alive and are now dead.</summary>
        public IReadOnlyList<int> ActorsKilled { get; }
    }

    /// <summary>
    /// Applies the defeat to one encounter record.
    /// </summary>
    /// <param name="states">The zone's object-state block.</param>
    /// <param name="refPair">Ref-pair the record belongs to.</param>
    /// <param name="recordIndex">Index of the record within the ref-pair.</param>
    /// <param name="roster">
    /// The record's seven roster actor ids, <see cref="EmptyRosterSlot"/> where empty.
    /// </param>
    /// <param name="isAlive">Whether a roster actor is still living.</param>
    /// <param name="kill">Marks a roster actor dead.</param>
    /// <remarks>
    /// <b>Already-dead actors are skipped, not re-killed.</b> The original tests the flag first and
    /// leaves the record untouched, which matters because writing it back would rewrite a combatant
    /// the party may have looted.
    ///
    /// <para><b>Every LIVING roster actor dies, whether or not it was on the field.</b> That is what
    /// makes a defeated encounter stay defeated: the roster is the source of truth a later visit
    /// re-seeds from, so leaving a survivor there would repopulate the group.</para>
    /// </remarks>
    public static Result ApplyToRecord(EncounterObjectStates states, int refPair, int recordIndex,
        IReadOnlyList<int> roster, Func<int, bool> isAlive, Action<int> kill) {
        if (states == null) {
            throw new ArgumentNullException(nameof(states));
        }
        if (isAlive == null) {
            throw new ArgumentNullException(nameof(isAlive));
        }
        if (kill == null) {
            throw new ArgumentNullException(nameof(kill));
        }

        int stopped = states.StopRoaming(refPair, recordIndex);

        var killed = new List<int>();
        if (roster != null) {
            for (var slot = 0; slot < roster.Count && slot < RosterSlots; slot++) {
                int actorId = roster[slot];
                if (actorId == EmptyRosterSlot || !isAlive(actorId)) {
                    continue;
                }
                kill(actorId);
                killed.Add(actorId);
            }
        }

        return new Result(1, stopped, killed);
    }
}
