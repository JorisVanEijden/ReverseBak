namespace GameData.Resources.World;

using GameData.Resources.Character;
using System.Collections.Generic;

/// <summary>
/// Walking into a pit — <c>worldcross_dungeon_descent_anim</c>
/// (<c>SRC/GAME/WORLD/WORLDCRS.C</c>). Terrain kind 15 is walkable on purpose: falling in is the
/// point.
///
/// <para><b>It does not take you to the level below</b>, despite reading that way and despite the
/// collision spec having said so. The camera is moved to the next type-0x0f entity in the SAME zone
/// and dropped; there is no zone or level transition anywhere in the routine. What ends the trip is
/// the party being flagged as down — see <see cref="PartyDeathStateOnFall"/>.</para>
///
/// <para>Distinct from <see cref="PitRopeCrossing"/>, which is the world OBJECT you click to swing
/// over a chasm. This is the <c>m_pit</c> POLYGON you walk onto.</para>
/// </summary>
public static class PitDescent {
    /// <summary>The walkable terrain kind that drops you.</summary>
    public const int PitTerrainKind = 0xf;

    /// <summary>
    /// <b>Pits only exist underground.</b> The whole routine is wrapped in a zone-kind test, so a
    /// kind-15 polygon in an outdoor zone does nothing at all. Same enclosed zone kind that gates
    /// doors and Candle Glow — see <c>DoorMechanics</c> and <c>SpellCasting</c>.
    /// </summary>
    public const int RequiredZoneKind = ZoneDefinition.UndergroundZoneLocation;

    /// <summary>Camera frames in the fall.</summary>
    public const int DescentSteps = 9;

    /// <summary>
    /// Frames when scancode 0x19 ('P') is held as you fall — a longer drop, for no stated reason.
    /// The original also then waits for a different key before continuing.
    /// </summary>
    public const int HeldKeyDescentSteps = 0xd;

    /// <summary>World units the camera drops per frame.</summary>
    public const int UnitsPerStep = 0x50;

    /// <summary>Sound effect played for the fall.</summary>
    public const int FallSoundId = 0x2f;

    /// <summary>Dialog shown on landing.</summary>
    public const int LandingDialogId = 0x115;

    /// <summary>
    /// The value the fall writes to the party-death byte — the field we model as
    /// <c>SaveGameFields.PartyDeathState</c> (save body offset 14).
    /// </summary>
    /// <remarks>
    /// <b>It is not a "world loop exit request", and the distinction is not cosmetic.</b> The
    /// original has TWO adjacent bytes and this writes the first: <c>bCombatExitRequest</c> at
    /// offset 14 (ours: <c>PartyDeathState</c>), not <c>nWorldLoopExitRequest</c> at offset 15
    /// (ours: <c>ChapterTransitionPending</c>). Wiring this constant into the byte its old name
    /// named would ask the world loop for a plain reload and leave the party un-flagged.
    ///
    /// <para><b>And it really is the party-death byte, used for exactly what it is for.</b> The
    /// stat code raises the same field to 1 when it NOTICES every active member has Near-death set,
    /// which is the route an ORDINARY party wipe takes — including one in the arena, because
    /// <c>combat_arena_actor_die</c> applies Near-death and the sweep does the rest. (The arena's own
    /// literal 2 is <c>combat_actor_kill_remaining_enc</c>, reached only from COMBAT.C case 16 behind
    /// <c>key_is_down(0x1d)</c> and a confirmation — a give-up command, not a killing blow. This
    /// comment used to call it "the arena kills the last of them", which named the rarer path.)
    /// The fall has itself put the
    /// whole party at full Near-death, so it writes <b>2</b> — asserting the state directly rather
    /// than waiting to be noticed. The 1/2 split matters downstream: 1 makes the map screen play
    /// dialog 0x145, and the pit skips that because it has already played
    /// <see cref="LandingDialogId"/>.</para>
    ///
    /// <para><b>There is no level change.</b> Nothing in the routine transitions zone or level — the
    /// camera is moved to the next type-0x0f entity in the SAME zone and dropped. "Exits to the
    /// level below" was in the collision spec and in this comment, and both were wrong.</para>
    /// </remarks>
    public const int PartyDeathStateOnFall = 2;

    /// <summary>
    /// What the fall does to the party.
    ///
    /// <para><b>Every active member is put at full Near-death, not merely damaged.</b> The original
    /// calls <c>stat_combatant_apply_condition(member, 6, 100)</c> — condition 6 is Near-death, not
    /// attribute 6 — so this is far more severe than a hit-point loss, and it lands on the whole
    /// party at once regardless of who was leading.</para>
    ///
    /// <para>Because raising Near-death <i>collapses</i> an actor (see <c>ConditionEngine</c>), the
    /// fall also <b>clears every other affliction</b> as a side effect: a poisoned, starving party
    /// lands near dead but otherwise clean. That is a consequence of the shared condition rule
    /// rather than anything pit-specific, and it is reproduced rather than special-cased.</para>
    /// </summary>
    /// <param name="party">Conditions of the active members; nulls are skipped.</param>
    /// <param name="stats">
    /// Optional matching health/stamina pairs. Supplying them lets the collapse zero and refill the
    /// pool as the original does; omit them and only the condition ranks change.
    /// </param>
    public static void ApplyToParty(IEnumerable<ActorConditions> party,
        IEnumerable<(ActorStat Health, ActorStat Stamina)> stats = null) {
        if (party == null) {
            return;
        }

        IEnumerator<(ActorStat Health, ActorStat Stamina)> pools = stats?.GetEnumerator();
        foreach (ActorConditions conditions in party) {
            bool hasPool = pools != null && pools.MoveNext();
            if (conditions == null) {
                continue;
            }
            if (hasPool) {
                ConditionEngine.Apply(conditions, ActorCondition.NearDeath, ActorConditions.MaxRank,
                    pools.Current.Health, pools.Current.Stamina);
            } else {
                ConditionEngine.Apply(conditions, ActorCondition.NearDeath, ActorConditions.MaxRank);
            }
        }
        pools?.Dispose();
    }

    /// <summary>
    /// How many frames the fall lasts.
    /// </summary>
    /// <param name="descendKeyHeld">Whether scancode 0x19 is down as the fall begins.</param>
    public static int StepsFor(bool descendKeyHeld) =>
        descendKeyHeld ? HeldKeyDescentSteps : DescentSteps;

    /// <summary>The camera's z offset from its starting height at a given frame of the fall.</summary>
    public static int DropAtStep(int step) => -step * UnitsPerStep;

    /// <summary>
    /// Whether stepping onto this terrain drops the party.
    ///
    /// <para>The original checks the <b>crossing</b> kind recorded on the previous loop iteration,
    /// not the kind under the party right now — you fall on the iteration <i>after</i> the crossing
    /// is recorded, which is what gives the step onto the pit a frame to render.</para>
    /// </summary>
    public static bool Triggers(int crossingKind, int zoneKind) =>
        zoneKind == RequiredZoneKind && crossingKind == PitTerrainKind;

    // ---------------------------------------------------------------- where the party lands

    /// <summary>
    /// Which entity the fall drops the party onto — <c>proxscan_paged_find_next_type0f</c>
    /// (PROXSCAN.C:279).
    /// </summary>
    /// <param name="visibleEntityKinds">
    /// The visibility pass's entry list, <b>in the order it appended them</b>, each entry's shape
    /// kind (<c>WorldEntityType</c>).
    /// </param>
    /// <returns>The index to land on, or <see cref="NoTarget"/>.</returns>
    /// <remarks>
    /// <b>THE NAME IS WRONG IN THREE WAYS, AND EACH ONE MISLEADS DIFFERENTLY.</b> There is no
    /// paging, no cursor and no "next":
    /// <code>
    /// for (i = g_nVisibleEntryCount - 1; i >= 0; i--)
    ///     if (ts_get_shape(entry[i]-&gt;shapeId)-&gt;kind == 0x0f) return 1;
    /// </code>
    /// It scans the visible-entity list <b>backwards</b> and takes the first match — so the answer is
    /// the <b>LAST</b> pit the visibility pass appended.
    ///
    /// <para><b>This is a PROXIMITY-FILTERED set, not the zone.</b> <c>proxscan_visibility</c>
    /// rebuilds the list every pass: each combat-zone region in order, then the world-object pool,
    /// keeping only entries inside their own kind's threshold from the party
    /// (<c>g_aFilterTable[kind]</c>). So the party lands on a pit that is <i>near</i> them, and the
    /// list is <b>appended in the zone's own entity order and never sorted</b> — the backwards scan
    /// is reverse insertion order, NOT "the farthest" or "the nearest".</para>
    ///
    /// <para><b>Recorded because this task was blocked on believing otherwise:</b> the order was
    /// written up as an unreproducible paged-list cursor, making the choice of pit look like a
    /// fidelity decision between inventing an order and modelling one. It is neither — the rule is
    /// ordinary and reproducible by anything that walks the same entity lists in the same order.</para>
    ///
    /// <para><b>The kind comes from the SHAPE record</b>, not from a tile's type id. A world tile's
    /// <c>TypeId</c> is an index into the zone's entity table; looking for kind 15 in the tile data
    /// finds nothing and concludes the zone has no pits.</para>
    /// </remarks>
    public static int SelectTarget(IReadOnlyList<int> visibleEntityKinds) {
        if (visibleEntityKinds == null) {
            return NoTarget;
        }
        for (int i = visibleEntityKinds.Count - 1; i >= 0; i--) {
            if (visibleEntityKinds[i] == PitTerrainKind) {
                return i;
            }
        }
        return NoTarget;
    }

    /// <summary>No entity to land on. <b>An ordinary outcome, not an error</b>.</summary>
    /// <remarks>
    /// The party still takes the condition, still hears <see cref="FallSoundId"/> and still reads
    /// <see cref="LandingDialogId"/>; only the camera move and the drop are skipped. So a pit with
    /// nothing to fall to still downs the party where they stand.
    /// </remarks>
    public const int NoTarget = -1;

    /// <summary>
    /// <b>A pending full redraw suppresses the descent — but nothing else.</b>
    /// </summary>
    /// <remarks>
    /// The original gates the lookup on <c>g_full_redraw_needed == 0</c>, so the same branch that
    /// handles "no pit found" also handles "the screen is about to be rebuilt". Both land in
    /// <see cref="NoTarget"/> territory, and the conditions, sound and dialog happen either way.
    /// Modelled rather than folded into the caller because it is the one input that makes an
    /// otherwise-valid target not be used.
    /// </remarks>
    public static bool DescentIsAnimated(bool fullRedrawPending, int target) =>
        !fullRedrawPending && target != NoTarget;
}
