namespace GameData.Resources.World;

using System.Collections.Generic;

/// <summary>
/// Putting a defeated encounter back — <c>rgnenc_complete_consume</c> (canassa RGNENC.C:522).
///
/// <para>The inverse of <see cref="EncounterDefeat"/>: it clears the encounter's "fought" flag and
/// the hotspot's <b>done</b>, <b>scout tried</b> and <b>scouted</b> flags, then reloads the
/// encounter's creature group. All four, not just the fought flag — leaving any of the other three
/// set produces an encounter that is armed again but that the party is still recorded as having
/// dealt with, so it never re-fires.</para>
/// </summary>
public static class EncounterReset {
    /// <summary>
    /// The two trigger types that carry an encounter record.
    /// </summary>
    /// <remarks>
    /// The original tests <c>wKind == 1 || wKind == 7</c> in four places — building the record list,
    /// seeding the object states, placing actors, and here. Those are
    /// <see cref="TileEventType.Comb"/> and <see cref="TileEventType.Trap"/>: both DEF record shapes
    /// carry a <c>recId</c>, an actor template and a flags byte, which is why the same code handles
    /// them interchangeably.
    /// </remarks>
    public static bool CarriesEncounter(TileEventType type) =>
        type == TileEventType.Comb || type == TileEventType.Trap;

    /// <summary>
    /// Encounter records one zone can hold.
    /// </summary>
    /// <remarks>
    /// The record list stops appending at five (<c>g_nEncounter_record_count &lt; 5</c>), and that is
    /// the same five the save block reserves per ref-pair — a zone cannot hold more encounters than
    /// there is state to remember them with. Expressed as the save block's number rather than a
    /// literal, so the two cannot drift apart.
    /// </remarks>
    public const int MaxRecordsPerZone = EncounterActorPersistence.RecordsPerRefPair;

    /// <summary>
    /// Which trigger in the zone's list belongs to encounter record <paramref name="recordIndex"/>,
    /// or -1 when there is none.
    /// </summary>
    /// <param name="triggerTypesInOrder">Every trigger in the zone, in list order.</param>
    /// <param name="recordIndex">The encounter record's index.</param>
    /// <remarks>
    /// <b>The correspondence is POSITIONAL, not by id.</b> The record list is built by walking the
    /// zone's triggers in order and appending each <see cref="CarriesEncounter"/> one, so record N
    /// is the Nth such trigger — and this routine re-walks the same list counting to the same N.
    /// A port that looked the trigger up by encounter id would clear the wrong hotspot whenever two
    /// records share an id, and would clear none at all for a record whose id is repeated.
    ///
    /// <para><b>Note the subsystem uses BOTH schemes.</b> <c>rgnenc_mark_defended</c> filters by
    /// encounter id while this counts by position, so neither one can be assumed from the other.</para>
    ///
    /// <para>Non-encounter triggers between them are skipped, so the returned index is generally
    /// NOT <paramref name="recordIndex"/>.</para>
    /// </remarks>
    public static int TriggerIndexForRecord(IReadOnlyList<TileEventType> triggerTypesInOrder,
        int recordIndex) {
        if (triggerTypesInOrder == null || recordIndex < 0) {
            return -1;
        }

        var seen = 0;
        for (var i = 0; i < triggerTypesInOrder.Count; i++) {
            if (!CarriesEncounter(triggerTypesInOrder[i])) {
                continue;
            }
            if (seen == recordIndex) {
                return i;
            }
            seen++;
        }
        return -1;
    }

    /// <summary>
    /// The per-hotspot flags a reset clears, alongside the encounter's own "fought" flag.
    /// </summary>
    /// <remarks>
    /// Named rather than keyed: the save-state key arithmetic belongs to the consumer that owns the
    /// flag space. This says WHICH flags, which is the part the original decides.
    /// </remarks>
    public enum ClearedFlag {
        /// <summary>The hotspot has already run.</summary>
        Done,

        /// <summary>The party has attempted to scout this hotspot.</summary>
        ScoutTried,

        /// <summary>The scouting attempt succeeded.</summary>
        Scouted,
    }

    /// <summary>Every per-hotspot flag a reset clears, in the order the original clears them.</summary>
    public static IReadOnlyList<ClearedFlag> ClearedHotspotFlags { get; } = new[] {
        ClearedFlag.Done,
        ClearedFlag.ScoutTried,
        ClearedFlag.Scouted,
    };
}
