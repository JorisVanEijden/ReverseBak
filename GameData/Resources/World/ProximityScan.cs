namespace GameData.Resources.World;

/// <summary>
/// Deciding what is near enough to matter as the party moves — <c>proxscan_encounter_records</c>
/// (<c>SRC/R3D/VIS/PROXSCAN.C</c>). It does two jobs in one pass: build the visible-entry list for
/// rendering, and fire the roaming-encounter proximity check.
/// </summary>
public static class ProximityScan {
    /// <summary>Entries the visible list can hold; the scan stops adding beyond this.</summary>
    public const int MaxVisibleEntries = 600;

    /// <summary>
    /// A filter-table threshold of -1 means <b>this kind is switched off entirely</b> at the
    /// current detail level — it is not a distance of -1.
    /// </summary>
    public const int DisabledThreshold = -1;

    /// <summary>
    /// A threshold of 1 means <b>always in range</b>. The original forces the metric to zero rather
    /// than comparing distances, so such a kind is never culled however far away it is. The shipped
    /// FILTER.DAT uses it for the first four kinds at every detail level.
    /// </summary>
    public const int AlwaysVisibleThreshold = 1;

    /// <summary>
    /// How near the party must pass for an entity to be written onto the automap, in world units.
    /// </summary>
    public const int AutomapProximityRange = 0x640;

    /// <summary>
    /// The zone kind the automap records in — the enclosed/underground one.
    /// <b>Nothing is recorded outdoors</b>, which is why the overworld map has no explored state.
    /// </summary>
    public const int AutomapZoneKind = ZoneDefinition.UndergroundZoneLocation;

    /// <summary>
    /// Whether an entity kind takes part in the scan at all. Anything outside this set is skipped
    /// before its distance is even measured.
    /// </summary>
    public static bool Participates(int kind) =>
        kind <= 4 || kind == 7 || kind == 10 || kind == 0xe || kind == 0xf
        || kind == 0x14 || kind == 0x17 || kind == 0x26 || kind == 0x27;

    /// <summary>
    /// The kinds the automap records: 14, the pit (15), the tunnel (20) and the door (23).
    ///
    /// <para>They are the <b>level-connection features</b> — the places you arrive at or leave
    /// through — which is exactly what a dungeon plan is worth drawing. It is not the whole
    /// participating set.</para>
    /// </summary>
    public static bool AppearsOnAutomap(int kind) =>
        kind == 0xe || kind == 0xf || kind == 0x14 || kind == 0x17;

    /// <summary>
    /// The distance an entity is culled on, after allowing for its own size.
    /// </summary>
    /// <param name="octagonalDistance">Party-to-entity distance, the octagonal approximation.</param>
    /// <param name="radius">The shape's radius.</param>
    /// <param name="shift">The shape's radius shift — radius is stored scaled.</param>
    /// <param name="threshold">This kind's filter-table entry at the current detail level.</param>
    /// <remarks>
    /// Subtracting <c>radius &lt;&lt; shift</c> is what lets a large object register from further
    /// away than a small one at the same threshold: the test is against the entity's edge, not its
    /// centre.
    /// </remarks>
    public static long CullingMetric(long octagonalDistance, int radius, int shift, long threshold) =>
        threshold == AlwaysVisibleThreshold ? 0 : octagonalDistance - ((long)radius << shift);

    /// <summary>Whether an entity joins the visible list.</summary>
    public static bool IsVisible(int kind, long octagonalDistance, int radius, int shift,
        long threshold, int visibleSoFar) {
        if (visibleSoFar >= MaxVisibleEntries || !Participates(kind)
            || threshold == DisabledThreshold) {
            return false;
        }
        return CullingMetric(octagonalDistance, radius, shift, threshold) < threshold;
    }

    /// <summary>
    /// Whether passing this entity writes it onto the dungeon automap.
    /// </summary>
    /// <remarks>
    /// <b>Renamed 2026-08-20; this was called <c>TriggersEncounter</c> and it raises no encounter.</b>
    /// The condition it models sits in the proximity scan's loop and its body is
    /// <c>rec_prox(&amp;list-&gt;bZone, i)</c> — the automap recorder, marking entity <c>i</c> of
    /// this chunk as seen. Both scan functions carry the same call and neither has an encounter
    /// path. The old name came from the buffer's canassa name ("encounter table") and would have
    /// sent an implementer to the wrong system; nothing consumed it but its own tests.
    /// See <see cref="DungeonAutomap"/>.
    ///
    /// <para><b>Measured on the raw distance, not the culling metric.</b> The entity's size is not
    /// allowed for here, so a large door and a small one record at the same range — deliberately
    /// different from the visibility test alongside it.</para>
    /// </remarks>
    public static bool RecordsOnAutomap(int kind, long octagonalDistance, int zoneKind,
        bool hasAutomapRecord) =>
        zoneKind == AutomapZoneKind
        && octagonalDistance < AutomapProximityRange
        && AppearsOnAutomap(kind)
        && hasAutomapRecord;
}
