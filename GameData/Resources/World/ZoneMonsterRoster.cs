namespace GameData.Resources.World;

using System.Collections.Generic;

/// <summary>
/// The creature types a zone offers in a given chapter — <c>Z##SHP.DAT</c>, read by
/// <c>rgnenc_load_zone_shape_index</c> (<c>SRC/GAME/ENC/RGNENC.C</c>).
///
/// <para><b>What the file really is: an animation-residency list.</b> The loader's only action is to
/// call <c>combat_actor_bnames_load_cached</c> on each entry, and the zone teardown releases the
/// same four — so the table names <i>which monster art stays loaded</i> for this zone and chapter,
/// not which monsters the game may spawn. The two coincide because a roaming encounter can only show
/// a creature whose art is resident, which is why it reads as a spawn roster.</para>
///
/// <para><b>The four are not the whole story, and the rest is not portable.</b> The engine keeps a
/// fifth slot for one creature an encounter needs that is not among the four, refilled and released
/// every time encounters are placed, and it collects at most ONE such extra
/// (<c>nShapeCount &lt; 1</c>). That is a 16-bit memory budget, not a game rule: a remake with
/// ordinary asset streaming has no reason to cap a zone at five creature types, so the fifth slot is
/// documented here and deliberately not modelled.</para>
///
/// <para><b>NO PRODUCTION CONSUMER BY DESIGN.</b> This records an animation-residency budget — a 16-bit
/// memory constraint the remake has no reason to reproduce, kept so the number is on
/// record rather than rediscovered. Nothing calls it and nothing should; the marker in
/// this sentence is what keeps it out of the unconsumed-models audit.</para>
/// </summary>
public static class ZoneMonsterRoster {
    /// <summary>Creature slots a zone offers per chapter.</summary>
    public const int SlotsPerChapter = 4;

    /// <summary>Chapters the file carries a row for.</summary>
    public const int ChapterCount = 9;

    /// <summary>
    /// Extra residency slots the engine keeps beyond <see cref="SlotsPerChapter"/> — one, holding a
    /// creature an encounter asked for that the roster does not list. Recorded for the record; the
    /// cap is a DOS memory budget and is not applied here.
    /// </summary>
    public const int EngineExtraSlots = 1;

    /// <summary>
    /// The row for a chapter. <paramref name="chapter"/> is <b>1-based</b> — the original seeks
    /// <c>(chapter - 1) * 8</c> bytes in, eight being four 16-bit slots.
    /// </summary>
    /// <returns><c>null</c> when the zone has no row for that chapter.</returns>
    public static ChapterMonsters For(ZoneShape zone, int chapter) =>
        zone?.Chapters != null && chapter >= 1 && chapter <= zone.Chapters.Count
            ? zone.Chapters[chapter - 1]
            : null;

    /// <summary>
    /// The creature types a zone offers in a chapter, in the file's own slot order.
    /// </summary>
    /// <remarks>
    /// <b>Empty slots are skipped, not stopped at.</b> The original tests all four independently
    /// rather than breaking on the first <see cref="CreatureType.None"/>, so a row with a gap in the
    /// middle would still load the entries after it. The shipped data happens to keep its empties
    /// trailing — 108 rows, no interior gap — but that is the data being tidy, not the format
    /// promising anything.
    /// </remarks>
    public static IReadOnlyList<CreatureType> TypesIn(ZoneShape zone, int chapter) {
        var types = new List<CreatureType>(SlotsPerChapter);
        ChapterMonsters row = For(zone, chapter);
        if (row == null) {
            return types;
        }
        AddIfPresent(types, row.Slot1);
        AddIfPresent(types, row.Slot2);
        AddIfPresent(types, row.Slot3);
        AddIfPresent(types, row.Slot4);

        return types;
    }

    /// <summary>
    /// Whether a creature is on the zone's roster for a chapter — the membership test an encounter
    /// placement does before deciding it needs the extra residency slot.
    /// </summary>
    public static bool Offers(ZoneShape zone, int chapter, CreatureType creature) =>
        creature != CreatureType.None && TypesIn(zone, chapter).Contains(creature);

    /// <summary>
    /// Whether a zone has anything roaming in a chapter at all.
    /// </summary>
    /// <remarks>
    /// <b>Most rows are empty</b> — 46 of the shipped 108 carry anything — so this is the common
    /// case rather than an edge one. A zone with an empty row is not broken; the party either cannot
    /// reach it in that chapter or meets nothing there.
    /// </remarks>
    public static bool HasAny(ZoneShape zone, int chapter) => TypesIn(zone, chapter).Count > 0;

    private static void AddIfPresent(List<CreatureType> types, CreatureType creature) {
        if (creature != CreatureType.None) {
            types.Add(creature);
        }
    }
}
