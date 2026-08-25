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
    /// The encounter record ids a zone-ref chunk holds, in <b>trigger order</b> — the original's
    /// <c>g_anEncounterRecordIds</c>.
    /// </summary>
    /// <param name="triggersInOrder">
    /// Every trigger in the chunk, in list order, with the encounter record id each one names.
    /// A trigger that names none (or that is not a <see cref="CarriesEncounter"/> kind) passes
    /// <c>null</c> and is skipped, exactly as the original's kind test skips it.
    /// </param>
    /// <remarks>
    /// <b>The list stops at <see cref="MaxRecordsPerZone"/> and that is not a formality.</b>
    /// <c>rgnenc_load_encounter_actors</c> appends only while
    /// <c>g_nEncounter_record_count &lt; 5</c>, so a sixth encounter in one chunk gets no entry —
    /// and therefore <b>no state slot</b>. Everything addressed through this list (placement,
    /// defeat, reset, the removal a death persists) simply does not apply to it. A port that let the
    /// list grow would compute a slot inside the NEXT ref pair's block and corrupt a different
    /// zone's encounters.
    /// </remarks>
    public static List<long> RecordIds(
        IEnumerable<(TileEventType Type, long? RecordId)> triggersInOrder) {
        var ids = new List<long>();
        if (triggersInOrder == null) {
            return ids;
        }
        foreach ((TileEventType type, long? recordId) in triggersInOrder) {
            if (ids.Count >= MaxRecordsPerZone) {
                break;
            }
            if (CarriesEncounter(type) && recordId.HasValue) {
                ids.Add(recordId.Value);
            }
        }
        return ids;
    }

    /// <summary>
    /// Which encounter record within the ref pair a given record id is, or -1 when it has none.
    /// </summary>
    /// <remarks>
    /// <b>This is the lookup every write to the encounter-state block starts with.</b> Both
    /// <c>rgnenc_persist_actor_removed</c> and <c>rgnenc_persist_actor_placed</c> open by scanning
    /// <c>g_anEncounterRecordIds</c> for the id and <b>return 0 without writing</b> when it is not
    /// there — so an encounter that never made the list is silently not persisted rather than
    /// persisted somewhere wrong. -1 carries that same "do not write" answer.
    ///
    /// <para><b>The first match wins.</b> Two triggers naming one encounter share a record index,
    /// which is right: they are two ways into the same group of actors.</para>
    /// </remarks>
    public static int RecordIndexOf(
        IEnumerable<(TileEventType Type, long? RecordId)> triggersInOrder, long recordId) =>
        RecordIds(triggersInOrder).IndexOf(recordId);

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
