namespace GameData.Resources.World;

/// <summary>
/// Deciding what is near enough to matter as the party moves — <c>SRC/R3D/VIS/PROXSCAN.C</c>.
/// </summary>
/// <remarks>
/// <b>THERE ARE TWO SCANS AND THEY ADMIT DIFFERENT KINDS.</b> They look almost identical and the
/// difference is one line:
/// <list type="bullet">
/// <item><b><c>proxscan_run</c></b> (line 72) builds the VISIBLE-ENTRY LIST that the renderer walks.
/// It skips <b>kind 7</b> and takes everything else whose filter threshold is not -1. No
/// whitelist — see <see cref="JoinsVisibleList"/>.</item>
/// <item><b><c>proxscan_encounter_records</c></b> (line 203) is the roaming-encounter pass. It
/// admits only <c>kind &lt;= 4 || 7 || 10 || 14 || 15 || 20 || 23 || 38 || 39</c> — and
/// <b>includes</b> kind 7, which the other one drops. See
/// <see cref="ParticipatesInEncounterScan"/>.</item>
/// </list>
///
/// <para>Getting these the wrong way round is not a subtle error: the whitelist leaves out kinds
/// 5, 6, 8, 9, 11-13, 16-19, 21, 22 and 24-42, which is most of the world's furniture. A visibility
/// gate built on it would hide nearly everything and then show the <c>db1..db8</c> records that
/// <c>proxscan_run</c> exists to drop.</para>
/// </remarks>
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
    /// Whether an entity kind takes part in the ROAMING-ENCOUNTER scan at all — the whitelist of
    /// <c>proxscan_encounter_records</c> (PROXSCAN.C:220-222). Anything outside this set is skipped
    /// before its distance is even measured.
    /// </summary>
    /// <remarks>
    /// <b>This is NOT the visibility rule</b>, however much the two functions resemble each other;
    /// see the class remarks and <see cref="JoinsVisibleList"/>. Renamed from <c>Participates</c> on
    /// 2026-09-12, when <see cref="IsVisible"/> was found to be built on it.
    /// </remarks>
    public static bool ParticipatesInEncounterScan(int kind) =>
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

    /// <summary>The kind the visible-entry scan drops outright — the <c>db1..db8</c> records.</summary>
    public const int NeverRenderedKind = 7;

    /// <summary>
    /// Whether an entity joins the VISIBLE-ENTRY LIST — <c>proxscan_run</c>, PROXSCAN.C:88-111.
    /// </summary>
    /// <remarks>
    /// <b>No kind whitelist.</b> The only kind excluded by identity is <see cref="NeverRenderedKind"/>;
    /// everything else is admitted unless FILTER.DAT switches it off with
    /// <see cref="DisabledThreshold"/>. That is the whole difference from
    /// <see cref="ParticipatesInEncounterScan"/>, and it is the difference between drawing the world
    /// and drawing almost none of it.
    /// </remarks>
    public static bool JoinsVisibleList(int kind, long octagonalDistance, int radius, int shift,
        long threshold, int visibleSoFar) {
        if (visibleSoFar >= MaxVisibleEntries || kind == NeverRenderedKind
            || threshold == DisabledThreshold) {
            return false;
        }
        return CullingMetric(octagonalDistance, radius, shift, threshold) < threshold;
    }

    /// <summary>
    /// Whether an entity joins the ROAMING-ENCOUNTER scan's list —
    /// <c>proxscan_encounter_records</c>, PROXSCAN.C:218-243.
    /// </summary>
    /// <remarks>
    /// <b>Was called <c>IsVisible</c>, and it never was that.</b> It is built on the encounter
    /// scan's kind whitelist, so as a visibility test it excluded most of the world and admitted the
    /// one kind <c>proxscan_run</c> drops. It had tests and no production caller, so nothing was
    /// broken by it — but it was about to be wired into rendering (TASK-436) when the two functions
    /// were read side by side.
    /// </remarks>
    public static bool JoinsEncounterScan(int kind, long octagonalDistance, int radius, int shift,
        long threshold, int visibleSoFar) {
        if (visibleSoFar >= MaxVisibleEntries || !ParticipatesInEncounterScan(kind)
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
    /// See <see cref="EncounterVisitTable"/> and <see cref="LocalMapScreen.DrawsDungeonAutomap"/>.
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
