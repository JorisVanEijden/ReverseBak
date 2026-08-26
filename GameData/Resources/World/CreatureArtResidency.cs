namespace GameData.Resources.World;

using System.Collections.Generic;

/// <summary>
/// Which creature types a chunk needs art for — the union of the zone's chapter roster and the
/// creatures its placed encounter actors actually use.
///
/// <para>The original builds this while placing (RGNENC.C:311-322): for each actor it places, it
/// checks the creature against the four slots of <see cref="ZoneMonsterRoster"/> and, if it is not
/// one of them, records it separately. <c>rgnenc_load_zone_shape_index</c> has already loaded the
/// four; this is what catches the fifth.</para>
/// </summary>
/// <remarks>
/// <b>The original caps the extras at ONE and we deliberately do not.</b> Its collection loop is
/// guarded by <c>nShapeCount &lt; 1</c>, so a chunk whose encounters name two creatures outside the
/// roster simply never loads the second one's art. That is a 16-bit memory budget — five creature
/// types' animation resident at once — and not a game rule: reproducing it would drop a monster's
/// sprite for reasons no player could interpret. <see cref="ZoneMonsterRoster.EngineExtraSlots"/>
/// keeps the number on record.
///
/// <para><b>The roster is residency, not permission.</b> Z##SHP.DAT names the art that stays loaded
/// for a zone and chapter; that it also predicts which monsters appear is a consequence of an
/// encounter only being able to show a creature whose art is resident. So an actor naming a creature
/// outside the roster is <b>ordinary</b> — it is what the fifth slot exists for — and not a data
/// error to reject.</para>
/// </remarks>
public static class CreatureArtResidency {
    /// <summary>
    /// Every creature type the chunk needs, roster first and then the extras in placement order.
    /// </summary>
    /// <param name="rostered">The zone/chapter roster — <see cref="ZoneMonsterRoster.TypesIn"/>.</param>
    /// <param name="placedCreatures">The creature number of each placed actor, in placement order.</param>
    /// <remarks>
    /// <b>Roster order first, and no duplicates.</b> The original tests the roster before its own
    /// extras list, so a creature already resident is never collected twice; keeping that order means
    /// a consumer loading in sequence warms the zone's own four before anything an encounter added.
    ///
    /// <para><b>Creature 0 is not a creature.</b> The original's collection is guarded by
    /// <c>*pActorDelta != 0</c> — an unset slot reads as 0, and treating it as a type would load art
    /// for whatever <see cref="CreatureType"/> happens to sit at zero.</para>
    /// </remarks>
    public static IReadOnlyList<CreatureType> Needed(IReadOnlyList<CreatureType> rostered,
        IEnumerable<int> placedCreatures) {
        var needed = new List<CreatureType>();
        if (rostered != null) {
            foreach (CreatureType type in rostered) {
                if (!needed.Contains(type)) {
                    needed.Add(type);
                }
            }
        }

        if (placedCreatures == null) {
            return needed;
        }

        foreach (int raw in placedCreatures) {
            if (raw == 0) {
                continue;
            }
            var type = (CreatureType)raw;
            if (!needed.Contains(type)) {
                needed.Add(type);
            }
        }
        return needed;
    }

    /// <summary>
    /// The creatures beyond the roster — what the original's fifth slot would have had to hold.
    /// </summary>
    /// <remarks>
    /// Worth having separately from <see cref="Needed"/>: <b>more than one of these means the
    /// original could not have shown them all</b>, and a chunk in that state is worth noticing even
    /// though we render it correctly.
    /// </remarks>
    public static IReadOnlyList<CreatureType> BeyondTheRoster(
        IReadOnlyList<CreatureType> rostered, IEnumerable<int> placedCreatures) {
        var extras = new List<CreatureType>();
        if (placedCreatures == null) {
            return extras;
        }

        foreach (int raw in placedCreatures) {
            if (raw == 0) {
                continue;
            }
            var type = (CreatureType)raw;
            if ((rostered == null || !rostered.Contains(type)) && !extras.Contains(type)) {
                extras.Add(type);
            }
        }
        return extras;
    }

    /// <summary>Whether this chunk asks for more art than the original could hold at once.</summary>
    /// <inheritdoc cref="BeyondTheRoster"/>
    public static bool ExceedsTheOriginalsBudget(IReadOnlyList<CreatureType> rostered,
        IEnumerable<int> placedCreatures) =>
        BeyondTheRoster(rostered, placedCreatures).Count > ZoneMonsterRoster.EngineExtraSlots;
}
