namespace GameData.Resources.Combat;

using System;
using System.Collections.Generic;

/// <summary>
/// Walking an actor across the combat grid toward its destination — the loop that spends a turn's
/// movement and fires whatever the actor walks onto.
/// </summary>
/// <remarks>
/// Ported from <c>combataipath_actor_walk_path</c> (<c>SRC/COMBAT/AI/CMBTAI.C</c>). This is the
/// caller <see cref="CombatMovement.Step"/>'s documentation has always referred to and nobody had
/// written: <c>Step</c> answers "where does one step land", and this answers "what happens over a
/// whole move".
///
/// <para><b>The hazards are the point.</b> They fire per step, on the tile just stepped ONTO, so a
/// path that crosses three crystal tiles hurts three times. A port that checks the destination
/// instead makes crossing a hazard field free as long as you stop somewhere safe.</para>
/// </remarks>
public static class CombatWalk {
    /// <summary>What an actor walked into.</summary>
    public enum HazardKind {
        /// <summary>Crystal ground went off underfoot.</summary>
        CrystalGround,

        /// <summary>A tile trap fired.</summary>
        TileTrap,

        /// <summary>A cannon had line on the tile and shot.</summary>
        CannonShot,
    }

    /// <summary>One hazard firing.</summary>
    public readonly struct Hazard {
        internal Hazard(HazardKind kind, int x, int y, int damage, int sourceX, int sourceY) {
            Kind = kind;
            X = x;
            Y = y;
            Damage = damage;
            SourceX = sourceX;
            SourceY = sourceY;
        }

        /// <summary>Which hazard.</summary>
        public HazardKind Kind { get; }

        /// <summary>The tile the actor was standing on.</summary>
        public int X { get; }

        /// <inheritdoc cref="X"/>
        public int Y { get; }

        /// <summary>
        /// Damage to apply, or 0 for a <see cref="HazardKind.CannonShot"/> — a cannon resolves a
        /// SPELL (<see cref="CannonLine.SpellId"/>) rather than dealing a number, so its magnitude
        /// is not this struct's to state.
        /// </summary>
        public int Damage { get; }

        /// <summary>For a cannon shot, the cannon's tile; otherwise the same as <see cref="X"/>.</summary>
        public int SourceX { get; }

        /// <inheritdoc cref="SourceX"/>
        public int SourceY { get; }
    }

    /// <summary>How a walk ended.</summary>
    /// <summary>An element the walk shoved out of its way.</summary>
    public readonly struct Shove {
        internal Shove(PushResult result, int fromX, int fromY, int toX, int toY) {
            Result = result;
            FromX = fromX;
            FromY = fromY;
            ToX = toX;
            ToY = toY;
        }

        /// <summary>What the shove did. <see cref="PushResult.CrystalFired"/> needs a caller.</summary>
        public PushResult Result { get; }

        /// <summary>The tile the element was on — where the actor now stands, if it moved.</summary>
        public int FromX { get; }

        /// <inheritdoc cref="FromX"/>
        public int FromY { get; }

        /// <summary>The tile it went to. Meaningless for a shove that did not move it.</summary>
        public int ToX { get; }

        /// <inheritdoc cref="ToX"/>
        public int ToY { get; }
    }

    public readonly struct WalkResult {
        internal WalkResult(int x, int y, int speedRemaining, bool arrived, bool pathClear,
            IReadOnlyList<Hazard> hazards, Shove? shove = null) {
            X = x;
            Y = y;
            SpeedRemaining = speedRemaining;
            Arrived = arrived;
            PathClear = pathClear;
            Hazards = hazards;
            Shove = shove;
        }

        /// <summary>Where the actor ended up — its start position again after a probe.</summary>
        public int X { get; }

        /// <inheritdoc cref="X"/>
        public int Y { get; }

        /// <summary>
        /// Movement left for the rest of the turn. <b>Unchanged by a probe.</b>
        /// </summary>
        public int SpeedRemaining { get; }

        /// <summary>Whether the actor reached the destination.</summary>
        public bool Arrived { get; }

        /// <summary>The original's return value: false once a step was refused outright.</summary>
        public bool PathClear { get; }

        /// <summary>Hazards that fired, in the order they did. Always empty for a probe.</summary>
        public IReadOnlyList<Hazard> Hazards { get; }

        /// <summary>
        /// The element this walk shoved, or null. At most one: a shove ends the move.
        /// </summary>
        public Shove? Shove { get; }
    }

    /// <summary>Walking onto crystal ground costs a flat 100, whatever else is true of the actor.</summary>
    public const int CrystalDamage = 100;

    /// <summary>
    /// Walks toward <paramref name="destX"/>,<paramref name="destY"/>, spending up to
    /// <paramref name="speed"/> steps.
    /// </summary>
    /// <param name="probe">
    /// <b>A dry run.</b> The original's <c>ranged</c> flag: the actor's position is restored at the
    /// end, no hazard fires, and the movement budget is not spent. Used with a scratch combatant to
    /// ask "could I get there" — so a port that fires hazards here damages actors for the AI merely
    /// thinking about a path.
    /// </param>
    /// <param name="puzzle">
    /// The trap puzzle, when the encounter is one. Only cannons need it; pass null for an ordinary
    /// fight, where no cannon can exist.
    /// </param>
    /// <param name="tileTrapDamage">
    /// A tile trap's damage, read from the TILE — it is per-tile data, not a constant. Omitted means
    /// no damage, and the trap still fires and still clears itself.
    /// </param>
    /// <param name="onHazard">
    /// Applies a hazard. The actor's death is read back from it, so this is also what lets a hazard
    /// stop the walk; without it the walk runs its full length and merely reports what it hit.
    /// </param>
    /// <param name="occupiedByLiveCombatant">
    /// Whether a tile holds a LIVING combatant, for cannon line-of-sight. <b>Omitting it means
    /// nobody provides cover</b>, which is right for a lone actor and wrong once a fight has sides —
    /// the grid's own occupancy cannot stand in, because it also carries elements and one element
    /// kind is deliberately transparent to a shot.
    /// </param>
    /// <remarks>
    /// <b>Pushable elements end the walk here rather than being shoved.</b> The original's step
    /// routine resolves the push inline; ours reports <see cref="StepStatus.BlockedByPushable"/> and
    /// leaves it to the caller, matching what <see cref="CombatMovement.Step"/> already documents.
    /// The shove itself is <c>TrapPuzzle.Push</c>. This is the one place the port stops short of the
    /// original's loop, and it is why <see cref="WalkResult.PathClear"/> is worth checking.
    /// </remarks>
    public static WalkResult Walk(CombatGrid grid, Combatant actor, int destX, int destY, int speed,
        bool probe = false, TrapPuzzle puzzle = null,
        Func<int, int, int> tileTrapDamage = null,
        Action<Combatant, Hazard> onHazard = null,
        Func<int, int, bool> occupiedByLiveCombatant = null) {
        if (grid == null) {
            throw new ArgumentNullException(nameof(grid));
        }
        if (actor == null) {
            throw new ArgumentNullException(nameof(actor));
        }

        var hazards = new List<Hazard>();
        int startX = actor.X;
        int startY = actor.Y;

        // *** Charged UP FRONT, from the straight-line distance. *** Not from the steps actually
        // taken: sliding around an obstacle is free, and being blocked outright still costs the full
        // distance to where the actor was trying to go.
        int distance = CombatGrid.ChebyshevDistance(startX, startY, destX, destY);
        int speedRemaining = speed - distance;

        // Gates pushing, and is fixed before the walk — so an element can only be shoved when the
        // actor STARTS adjacent to its destination, not when it becomes adjacent along the way.
        bool adjacentToTarget = distance == 1;

        int steps = speed;
        var pathClear = true;
        Shove? shove = null;

        while (steps != 0) {
            StepResult step = CombatMovement.Step(
                grid, actor.X, actor.Y, destX, destY, adjacentToTarget, probe);
            pathClear = step.Succeeded;

            if (step.Status == StepStatus.BlockedByPushable) {
                shove = Shoves(actor, step, puzzle, probe);
                break;
            }

            actor.X = step.X;
            actor.Y = step.Y;

            // Arrival ends the walk — but the hazard below still fires on the arrival tile, because
            // the original zeroes the counter and then falls into the switch in the same iteration.
            if (actor.X == destX && actor.Y == destY) {
                steps = 0;
            } else {
                steps--;
            }

            if (probe) {
                continue;
            }

            FireTerrainHazard(grid, actor, hazards, tileTrapDamage, onHazard);
            FireCannons(puzzle, actor, hazards, onHazard, occupiedByLiveCombatant);

            if (actor.IsDead) {
                steps = 0;
            }
        }

        if (probe) {
            actor.X = startX;
            actor.Y = startY;
            return new WalkResult(startX, startY, speed, false, pathClear, hazards);
        }

        return new WalkResult(actor.X, actor.Y, speedRemaining > 0 ? speedRemaining : 0,
            actor.X == destX && actor.Y == destY, pathClear, hazards, shove);
    }

    /// <summary>
    /// Shove the element the step ran into, and step onto the tile it leaves.
    /// </summary>
    /// <remarks>
    /// <b>The push lives HERE, one level above the step primitive, because that is where the
    /// original keeps it.</b> <c>moveCombatActorTowardTarget</c> @0x64bc1 tests the blocked cell for
    /// <c>grid_element_trap_diamond</c> and calls <c>PushTrapElement</c> itself;
    /// <c>CombatMovement.Step</c>'s counterpart never sees a shove. This method is what
    /// <see cref="CombatWalk"/>'s old remark called stopping short of the original's loop.
    ///
    /// <para>Three things the original does that are easy to miss, all kept:</para>
    /// <list type="number">
    /// <item><b>It only shoves when the actor started adjacent to its destination</b> —
    /// <c>combat_walkTargetAdjacent</c>, already computed as <c>adjacentToTarget</c> by the caller
    /// and folded into <see cref="CombatMovement.Step"/>'s decision to report a pushable at all.</item>
    /// <item><b>The actor's position is advanced optimistically and reverted when the push
    /// fails</b>, with <c>UI_ShowPathIsBlocked</c> shown. We never advance it early, so the revert
    /// is simply not stepping — same end state, one fewer way to get it wrong.</item>
    /// <item><b>A successful shove ENDS the move</b> (<c>return 1</c>), it does not spend one step
    /// and walk on. The caller breaks out of the loop either way.</item>
    /// </list>
    ///
    /// <para><b>A probe never shoves.</b> A dry run that moved objects would rearrange the puzzle
    /// for whoever asked "could I get there". The original takes an earlier exit for the same case.
    /// <i>Unverified:</i> it answers 1 there, i.e. reachable, where we leave <c>PathClear</c> false
    /// — the flag it tests is not identified, so this is left as it was rather than changed on a
    /// guess.</para>
    ///
    /// <para><b>The cannon check the original runs afterwards is deliberately not modelled.</b> It
    /// aims at the tile the DIAMOND landed on, not the actor's, and it gets there by standing a
    /// phantom actor on it — so the spell it fires lands on a throwaway and damages nobody
    /// (<c>cannon_fireAtCellViaPhantomActor?</c> @0x2fbac). Routing it through the hazard path would
    /// shoot the WALKER for a cannon aimed at an object. Whether the firing has effects beyond the
    /// damage is an open question on <c>Cast_Spell</c> and is recorded there.</para>
    /// </remarks>
    private static Shove? Shoves(Combatant actor, StepResult step, TrapPuzzle puzzle,
        bool probe) {
        if (probe || puzzle == null) {
            return null;
        }

        int dx = step.X - actor.X;
        int dy = step.Y - actor.Y;
        PushResult result = puzzle.TryPush(step.X, step.Y, dx, dy);
        // Occupancy is the caller's, exactly as it is for an ordinary step — CombatRuntime puts the
        // combatant back and re-applies through MoveTo. TryPush has already cleared the ELEMENT's
        // tile, which is the puzzle's to own.
        if (result == PushResult.Moved || result == PushResult.CrystalFired) {
            actor.X = step.X;
            actor.Y = step.Y;
        }

        return new Shove(result, step.X, step.Y, step.X + dx, step.Y + dy);
    }

    private static void FireTerrainHazard(CombatGrid grid, Combatant actor, List<Hazard> hazards,
        Func<int, int, int> tileTrapDamage, Action<Combatant, Hazard> onHazard) {
        CombatTerrain terrain = grid.TerrainAt(actor.X, actor.Y);
        Hazard hazard;

        switch (terrain) {
            case CombatTerrain.Crystal:
                // The sweep either side of this is a visual: the run is flipped to a lit kind and
                // straight back, so nothing reads it in between. Only the damage lands, and the
                // ground is NOT consumed — crossing it again hurts again.
                hazard = new Hazard(HazardKind.CrystalGround, actor.X, actor.Y, CrystalDamage,
                    actor.X, actor.Y);
                break;

            case CombatTerrain.Trap:
                // *** ONE-SHOT, unlike the crystal. *** The trigger clears the tile's effect, so the
                // tile is spent. Opposite persistence to crystal ground, on the same switch.
                int damage = tileTrapDamage?.Invoke(actor.X, actor.Y) ?? 0;
                hazard = new Hazard(HazardKind.TileTrap, actor.X, actor.Y, damage, actor.X, actor.Y);
                grid.SetTerrain(actor.X, actor.Y, CombatTerrain.Open);
                break;

            default:
                return;
        }

        hazards.Add(hazard);
        onHazard?.Invoke(actor, hazard);
    }

    private static void FireCannons(TrapPuzzle puzzle, Combatant actor, List<Hazard> hazards,
        Action<Combatant, Hazard> onHazard, Func<int, int, bool> occupiedByLiveCombatant) {
        if (puzzle == null) {
            return;
        }

        foreach (CannonLine.Shot shot in
                 CannonLine.ShotsOn(puzzle, actor.X, actor.Y, occupiedByLiveCombatant)) {
            var hazard = new Hazard(HazardKind.CannonShot, actor.X, actor.Y, 0, shot.X, shot.Y);
            hazards.Add(hazard);
            onHazard?.Invoke(actor, hazard);
        }
    }
}
