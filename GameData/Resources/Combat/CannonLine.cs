namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// Cannons on a trap-puzzle grid, and when one has line on an actor.
/// </summary>
/// <remarks>
/// <b>Cannons DO fire, and this task previously concluded they do not.</b> The earlier sweep looked
/// for reads of terrain 10-13 and found only three — the loader, the writer and the renderer's yaw
/// pick — and closed the item as "scenery that blocks a tile and points somewhere". The read it
/// could not find is a comparison against a PARAMETER: <c>combatgrid_tile_passable_check</c> tests
/// <c>terrain == required_terrain</c>, and the constants live at its only call site,
/// <c>combatgrid_step_search</c>, which passes them as DIRECTION codes. Nothing in either function
/// mentions a cannon, so a search by terrain id cannot turn it up.
///
/// <para><b>The direction code and the cannon's terrain id are the same number.</b> That is the
/// trick the original is playing: scanning west looks for terrain 11, east for 10, north for 12,
/// south for 13 — each scan hunting the one cannon kind that could be pointing back down it.</para>
///
/// <para>The firing itself is <c>cspell_apply_step_tile_spell(mover, 4, 0x14, -2)</c>: it builds a
/// throwaway caster standing ON THE CANNON'S TILE and resolves spell 4 at intensity 20 against the
/// walker. Placing the stub at the cannon rather than at the victim is what makes any
/// distance-dependent part of the effect measure from the cannon, so this is a shot from there and
/// not damage applied in place.</para>
/// </remarks>
public static class CannonLine {
    /// <summary>A cannon that has line on the tile that was asked about.</summary>
    public readonly struct Shot {
        internal Shot(CombatTerrain cannon, int x, int y) {
            Cannon = cannon;
            X = x;
            Y = y;
        }

        /// <summary>Which of the four cannon terrains it is.</summary>
        public CombatTerrain Cannon { get; }

        /// <summary>The cannon's tile — where the original stands its stub caster.</summary>
        public int X { get; }

        /// <inheritdoc cref="X"/>
        public int Y { get; }
    }

    /// <summary>The spell a cannon casts.</summary>
    public const int SpellId = 4;

    /// <summary>The intensity it casts at.</summary>
    public const int Intensity = 0x14;

    /// <summary><c>sound_arrow</c> (1) — the cannon going off, played at the muzzle.</summary>
    /// <remarks>
    /// <b>A cannon shot is heard TWICE on this id, and that is the original's.</b>
    /// <c>combatgrid_actor_step_to_tile</c> plays it before it resolves anything, and the cast it
    /// then resolves flies a projectile, whose own launch plays id 1 again
    /// (<see cref="SpellProjectileSound.LaunchCue"/>, reached because Flamecast's
    /// <c>AnimationEffectType</c> is 3). Two separate call sites, not one heard twice — do not
    /// "deduplicate" them.
    /// </remarks>
    public const int MuzzleCue = 1;

    /// <summary><c>sound_select</c> (10) — the tile cast itself, after the muzzle.</summary>
    /// <remarks>
    /// <b>The cue belongs to the tile cast, not to the cannon.</b>
    /// <c>cspell_apply_step_tile_spell</c> (CSPELL.C:1130) plays it, and that routine has a second
    /// caller — a spell whose flight was intercepted mid-air re-resolves through it
    /// (<see cref="SpellReflection"/>, which plays this same constant). Both are the same noise
    /// because they are the same routine.
    ///
    /// <para><b>Its one guard is spell 42.</b> The whole routine, cue included, is skipped when the
    /// spell is Strength Drain (0x2a). A cannon hard-codes spell <see cref="SpellId"/>, so the guard
    /// can never fire on this path — it exists for the interception path, where the spell is
    /// whatever was cast.</para>
    /// </remarks>
    public const int FireCue = 10;

    /// <summary>
    /// The element id that does <b>not</b> stop a cannon's line, unlike every other element.
    /// </summary>
    /// <remarks>
    /// The scan stops at any element whose id is not this one. Both 9 and 10 are pushables, so the
    /// two halves of one pair behave differently here — 9 blocks a shot and 10 does not. That
    /// asymmetry is the original's; it is not a tidy "pushables are transparent" rule, and it is why
    /// this is an element-id test rather than a terrain test.
    /// </remarks>
    public const int TransparentElementId = 10;

    /// <summary>Which cannon terrain a scan in each direction is hunting, keyed by that terrain's id.</summary>
    private static readonly (int Dx, int Dy, CombatTerrain Wanted)[] Scans = {
        (-1, 0, CombatTerrain.CannonEast),   // 11
        (1, 0, CombatTerrain.CannonWest),    // 10
        (0, -1, CombatTerrain.CannonNorth),  // 12
        (0, 1, CombatTerrain.CannonSouth),   // 13
    };

    /// <summary>
    /// Which way a cannon shoots, derived from the scan that finds it.
    /// </summary>
    /// <remarks>
    /// <b>Nothing in this build reads it, and that is the finding.</b> The wiki-level rules say
    /// "the posts can be deactivated if hit by a cannon's fireball", so a first pass walked the
    /// shot outward from the cannon and collapsed the post it reached. That mechanism is not in
    /// V102CD: the cannon fires <see cref="SpellId"/> (Flamecast), whose <c>AnimationEffectType</c>
    /// is <b>3</b>, and the only branch that collapses a run —
    /// <c>combat_actor_tile_entry_effect</c> case 3's <c>else</c>, which calls
    /// <c>combatgrid_shove_until_unblocked</c> — is reached only when the entity's
    /// <c>shapeId</c> is <b>2</b>. <c>projectile.base.shapeId = action_id</c>
    /// (WORLDHIT.C:627), so a Flamecast projectile never takes it. The one shipped spell with
    /// animation 2 is Despair Thy Eyes; Black Nimbus reaches the collapse by a different route
    /// (<c>combatgrid_push_back_actor(g_cursor_tile_x, g_cursor_tile_y)</c>, CSPELL.C:876).
    ///
    /// <para>Kept because the direction itself is measured and awkward to re-derive — the four
    /// terrain names do not agree with the grid's y direction — and because the collapse question
    /// will be asked again. The answer is here with its evidence rather than in a task note.</para>
    /// </remarks>
    public static (int Dx, int Dy) FiringDirection(CombatTerrain cannon) {
        foreach ((int dx, int dy, CombatTerrain wanted) in Scans) {
            if (wanted == cannon) {
                return (-dx, -dy);
            }
        }

        return (0, 0);
    }

    /// <summary>
    /// Every cannon that can see <paramref name="x"/>,<paramref name="y"/> — checked after each step
    /// of a walk, so all four directions fire in the same instant if all four have line.
    /// </summary>
    /// <param name="occupiedByLiveCombatant">
    /// Whether a tile holds a LIVING combatant. Dead ones do not block: the original's blocking test
    /// returns false for a corpse, so a body on the floor is no cover.
    /// </param>
    /// <remarks>
    /// <b>The scan starts on the NEXT tile, so an actor standing on a cannon is not shot by it.</b>
    ///
    /// <para>What stops a scan, in the original's order: the cannon it wants (a shot), a living
    /// combatant, or an element that is not <see cref="TransparentElementId"/>. Everything else is
    /// walked through — including empty crystal ground, which does not shield.</para>
    /// </remarks>
    public static IReadOnlyList<Shot> ShotsOn(TrapPuzzle puzzle, int x, int y,
        System.Func<int, int, bool> occupiedByLiveCombatant = null) {
        var shots = new List<Shot>();
        if (puzzle == null) {
            return shots;
        }

        foreach ((int dx, int dy, CombatTerrain wanted) in Scans) {
            int tx = x;
            int ty = y;
            while (true) {
                tx += dx;
                ty += dy;
                if (!CombatGrid.InBounds(tx, ty)) {
                    break;
                }
                if (puzzle.Grid.TerrainAt(tx, ty) == wanted) {
                    shots.Add(new Shot(wanted, tx, ty));
                    break;
                }
                if (occupiedByLiveCombatant != null && occupiedByLiveCombatant(tx, ty)) {
                    break;
                }
                TrapGridElement element = puzzle.ElementAt(tx, ty);
                if (element != null && element.ElementId != TransparentElementId) {
                    break;
                }
            }
        }

        return shots;
    }

    /// <summary>
    /// <b>The compass names on <see cref="CombatTerrain"/> do not follow one convention and must not
    /// be read as aim directions.</b>
    /// </summary>
    /// <remarks>
    /// Taking the scans at face value: the cannon found by scanning WEST is named CannonEast, so
    /// that pair is named for where it AIMS; the one found by scanning NORTH is named CannonNorth,
    /// so that pair is named for where it SITS. Both cannot be right. The data does not settle it
    /// either — all four terrains draw one model at four yaws — so the names remain the
    /// interpretation this task already flagged them as, and code should switch on the terrain
    /// value rather than trust the word.
    /// </remarks>
    public static bool CompassNamesAreReliable => false;
}
