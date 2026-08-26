namespace GameData.Resources.World;

using Data;

/// <summary>
/// Putting one encounter actor on the field — the placement loop of
/// <c>rgnenc_load_encounter_actors</c> (canassa RGNENC.C:216-260).
///
/// <para>One actor at a time, so the rule can be read and tested on its own; the caller walks the
/// chunk's encounter records and each record's roster. The seed pass that decides which slots are
/// <see cref="EncounterActorSpawn.Pending"/> in the first place is
/// <see cref="EncounterActorSpawn.SeedsAsPending"/> and runs before this.</para>
/// </summary>
/// <remarks>
/// <b>Everything is measured from the PARTY's tile, not the encounter's.</b> The original reads the
/// party tile once and uses it as the origin for every actor it places, template and stored pose
/// alike — the same tile-relative convention as the encounter landings, the hotspot box and the
/// corpse container. (canassa's locals for it are named <c>bZoneY</c>/<c>bZoneX</c> and receive x
/// and y respectively; the swap is in the names only.)
/// </remarks>
public static class EncounterActorPlacement {
    /// <summary>
    /// Actors that can be on the field across the whole chunk at once — the original's
    /// <c>g_nFixed_object_count &lt; 0x23</c>.
    /// </summary>
    /// <remarks>
    /// <b>The same 0x23 that is the per-ref-pair stride in the save block</b>
    /// (<see cref="EncounterObjectStates.EntriesPerRefPair"/>), and for the same reason: five
    /// records of seven slots. Expressed through it rather than restated so the two cannot drift.
    ///
    /// <para>The cap is on the CHUNK, not the record — a caller that applied it per record would
    /// let five full encounters place 35 actors.</para>
    /// </remarks>
    public const int MaxPlacedPerChunk = EncounterObjectStates.EntriesPerRefPair;

    /// <summary>One actor, placed.</summary>
    public readonly struct Placed {
        public Placed(int rosterSlot, int creatureNumber, long worldX, long worldY, short facing,
            bool roams, RoamingMovement.Pattern pattern = RoamingMovement.Pattern.Stationary,
            long[] waypointX = null, long[] waypointY = null) {
            RosterSlot = rosterSlot;
            CreatureNumber = creatureNumber;
            WorldX = worldX;
            WorldY = worldY;
            Facing = facing;
            Roams = roams;
            Pattern = pattern;
            WaypointX = waypointX ?? System.Array.Empty<long>();
            WaypointY = waypointY ?? System.Array.Empty<long>();
        }

        /// <summary>Which route this actor walks, from its template slot.</summary>
        public RoamingMovement.Pattern Pattern { get; }

        /// <summary>
        /// The route's waypoints, <b>already rebased into world coordinates</b>.
        /// </summary>
        /// <remarks>
        /// <b>THE REBASE IS THE WHOLE REASON THE ARRIVAL TEST WORKS.</b> The record stores them
        /// tile-relative, like the primary spawn — measured across the shipped data, every waypoint a
        /// moving pattern actually reads is in <c>[0, 64000)</c>. The original adds the party-tile
        /// origin to all four at placement time (RGNENC.C:280-283) and the updater then compares them
        /// against the actor's ABSOLUTE position by exact equality. Left tile-relative, that
        /// comparison can only ever match in tile (0, 0), and every patrol elsewhere walks off in a
        /// straight line forever — which is exactly the failure
        /// <see cref="RoamingMovement.IsAtWaypoint"/> warns about.
        ///
        /// <para><b>The original rebases a COPY of the template</b>, taken per placement. Doing it in
        /// place would add the origin again on the next chunk, and the route would drift a tile
        /// further away every time the party walked back.</para>
        /// </remarks>
        public System.Collections.Generic.IReadOnlyList<long> WaypointX { get; }

        /// <inheritdoc cref="WaypointX"/>
        public System.Collections.Generic.IReadOnlyList<long> WaypointY { get; }

        /// <summary>Index within the record's seven slots.</summary>
        public int RosterSlot { get; }

        /// <summary>Which creature — <c>mnames</c>, from the record's template.</summary>
        public int CreatureNumber { get; }

        public long WorldX { get; }

        public long WorldY { get; }

        public short Facing { get; }

        /// <summary>
        /// Whether this one walks.
        /// </summary>
        /// <remarks>
        /// <b>Only a roaming actor is ever updated</b> — the movement pass ignores every other kind,
        /// so a patrol pattern authored on a standing actor does nothing at all.
        /// </remarks>
        public bool Roams { get; }
    }

    /// <summary>
    /// Places one actor, or reports that this slot puts nothing on the field.
    /// </summary>
    /// <param name="stateWord">The slot's word from the save block.</param>
    /// <param name="standingOnly">The record's flag bit 0 — see <see cref="EncounterActorSpawn.IsPlaced"/>.</param>
    /// <param name="slot">The record's template entry for this slot.</param>
    /// <param name="stored">The slot's saved pose, used by everything that is not pending.</param>
    /// <param name="partyTileX">The party's tile, the origin for every actor placed this pass.</param>
    /// <param name="partyTileY"><inheritdoc cref="TryPlace" path="/param[@name='partyTileX']"/></param>
    /// <param name="frameRoll"><c>RND(3)</c>, used only when the actor is pending.</param>
    /// <param name="directionRoll"><c>RND2(2)</c>, likewise.</param>
    /// <param name="placed">The actor, when this returns true.</param>
    /// <param name="stateWordAfter">
    /// The word to write back. <b>Differs from <paramref name="stateWord"/> only for a pending
    /// actor</b>, which becomes roaming with a random stride; everything else is unchanged, and a
    /// caller that wrote it back unconditionally would be rewriting entries it did not touch.
    /// </param>
    /// <remarks>
    /// <b>The template is for a FIRST placement only.</b> A pending actor takes the record's authored
    /// spawn; one that has been placed before resumes from its stored pose, which is what makes a
    /// roamer pick up where it was left rather than snapping back to its authored post every time the
    /// party re-enters the chunk.
    /// </remarks>
    public static bool TryPlace(int stateWord, bool standingOnly, EnemySlot slot,
        EncounterObjectStates.Entry stored, int partyTileX, int partyTileY,
        int frameRoll, int directionRoll,
        out Placed placed, out int stateWordAfter) {
        placed = default;
        stateWordAfter = stateWord;

        if (!EncounterActorSpawn.IsPlaced(stateWord, standingOnly)) {
            return false;
        }

        long originX = (long)partyTileX * WorldPlacement.TileSize;
        long originY = (long)partyTileY * WorldPlacement.TileSize;

        long x;
        long y;
        short facing;
        if (EncounterActorSpawn.PlacesFromTemplate(stateWord)) {
            if (slot == null) {
                return false;
            }
            x = originX + slot.PrimarySpawnX;
            y = originY + slot.PrimarySpawnY;
            facing = slot.PrimaryRotationZ;
            stateWordAfter = EncounterActorSpawn.FreshlyPlacedState(frameRoll, directionRoll);
        } else {
            x = originX + stored.WorldXOffset;
            y = originY + stored.WorldYOffset;
            facing = stored.Facing;
        }

        var pattern = (RoamingMovement.Pattern)(slot?.MovementPattern ?? 0);
        int waypoints = RoamingMovement.WaypointCount(pattern);
        var wx = new long[waypoints];
        var wy = new long[waypoints];
        for (var i = 0; i < waypoints; i++) {
            wx[i] = originX + (slot.AltSpawnX != null && i < slot.AltSpawnX.Length ? slot.AltSpawnX[i] : 0);
            wy[i] = originY + (slot.AltSpawnY != null && i < slot.AltSpawnY.Length ? slot.AltSpawnY[i] : 0);
        }

        placed = new Placed(0, slot?.CreatureNumber ?? 0, x, y, facing,
            EncounterActorSpawn.KindOf(stateWordAfter)
                == EncounterActorSpawn.KindOf(EncounterActorSpawn.Roaming),
            pattern, wx, wy);
        return true;
    }

    /// <summary>
    /// <inheritdoc cref="TryPlace(int, bool, EnemySlot, EncounterObjectStates.Entry, int, int, int, int, out Placed, out int)"/>
    /// </summary>
    /// <param name="rosterSlot">Which of the record's seven slots this is, carried into the result.</param>
    /// <inheritdoc cref="TryPlace(int, bool, EnemySlot, EncounterObjectStates.Entry, int, int, int, int, out Placed, out int)"/>
    public static bool TryPlace(int rosterSlot, int stateWord, bool standingOnly, EnemySlot slot,
        EncounterObjectStates.Entry stored, int partyTileX, int partyTileY,
        int frameRoll, int directionRoll,
        out Placed placed, out int stateWordAfter) {
        if (!TryPlace(stateWord, standingOnly, slot, stored, partyTileX, partyTileY,
                frameRoll, directionRoll, out Placed bare, out stateWordAfter)) {
            placed = default;
            return false;
        }

        placed = new Placed(rosterSlot, bare.CreatureNumber, bare.WorldX, bare.WorldY, bare.Facing,
            bare.Roams, bare.Pattern, System.Linq.Enumerable.ToArray(bare.WaypointX),
            System.Linq.Enumerable.ToArray(bare.WaypointY));
        return true;
    }
}
