namespace GameData.Resources.Combat;

using System;
using System.Collections.Generic;

/// <summary>
/// The props of a trap arena rising out of the ground as the fight opens —
/// <c>combat_arena_burn_terr_cutsc</c> (canassa COMBAT.C:718).
/// </summary>
/// <remarks>
/// <b>Every prop rises on its own clock.</b> The duration is rolled PER TILE, uniform in
/// <see cref="MinimumDuration"/>..<see cref="MaximumDuration"/> inclusive, and the sweep decrements
/// all of them together — so they share a rate and finish at different moments. Filmed in the
/// original on encounter 347 (2026-09-06, 140 frames via the trap spike's frame dumper) the two
/// nearest props settled six frames apart, which is what two draws from that range look like at
/// <see cref="TicksPerFrame"/> ticks a frame. A port driving every prop off one clock would be
/// visibly wrong in that very encounter, which has four of them.
///
/// <para><b>It plays for a puzzle arena, not for every fight.</b> The gate is
/// <c>combatgrid_any_terrain_6</c> — literally "does any tile have terrain 6" — and terrain 6 is the
/// puzzle's exit. See <see cref="PlaysFor"/>.</para>
///
/// <para><b>The terrain must be put back.</b> Painting the effect overwrites the cell's kind, and
/// the tick's expiry rule turns a 9 into <see cref="CombatTerrain.Crystal"/> — so a cannon cell,
/// whose kind IS its facing (10..13), would come out of the animation as crystal ground and aim
/// north forever. The original backs the whole grid up before it starts and restores it cell by cell
/// afterwards; <see cref="Begin"/> and <see cref="Finish"/> are that backup.</para>
///
/// <para><b>No clip is ported.</b> The original clips the viewport at the projected ground point
/// because it has no depth buffer. A renderer with an opaque floor and a depth test hides a buried
/// prop for free — see <see cref="BurialAt"/>.</para>
/// </remarks>
public sealed class TrapPropEmergence {
    /// <summary>Shortest a prop can take to surface — <c>RNDR(0x190, 0x2bb)</c>'s low bound.</summary>
    public const int MinimumDuration = 0x190;

    /// <summary>Longest — and inclusive, because <c>RNDR</c> is (RAND.H:33).</summary>
    public const int MaximumDuration = 0x2bb;

    /// <summary>
    /// Effect ticks the cutscene runs per rendered frame — its inner
    /// <c>for (i = 0; i &lt; 15; i++) cspell_tick_damage_terrain()</c>.
    /// </summary>
    public const int TicksPerFrame = 15;

    /// <summary>
    /// How long one of those cutscene frames lasts.
    /// </summary>
    /// <remarks>
    /// <b>The tick count is the original's; the clock is ours, deliberately.</b> The original ticks
    /// per RENDERED frame, which ties the animation's length to how fast the machine draws — at 60
    /// fps that is a fifth of a second per prop and the whole thing is over before it reads as
    /// motion. Pacing it in real time instead keeps the duration the original has at any frame rate.
    ///
    /// <para>0.087 s is the arena's frame cadence measured off the running game (encounter 347, 140
    /// consecutive frames, 2026-09-06). It is the emulator's throughput rather than a number from
    /// the DOS timing code — which is the only measurement available — and it puts the emergence at
    /// 2.3 s for the shortest roll and 4.0 s for the longest, matching what those frames show.</para>
    /// </remarks>
    public const float CutsceneFrameSeconds = 0.087f;

    /// <summary>
    /// The kind painted on a rising cell. Shared with the Black Slayer's revival
    /// (<see cref="SlayerRevival.RisenTileEffect"/>) — one mechanism, two users, which is why the
    /// expiry rule that singles it out lives on <see cref="CombatGrid"/> rather than in either.
    /// </summary>
    public const int RisingKind = CombatGrid.RevertsToCrystalKind;

    private readonly List<(int X, int Y, CombatTerrain Terrain)> _restore = new();

    private float _elapsed;

    /// <summary>Whether any prop is still on its way up.</summary>
    public bool Running { get; private set; }

    /// <summary>
    /// Whether an element id names one of the props that rises.
    /// </summary>
    /// <remarks>
    /// <b>The original's predicate for this is called <c>combatgrid_is_combatant_type</c></b>
    /// (CMBTGRID.C:1100) and selects {7, 8, 9, 10, 0xb, 0x28} — the crystals, diamonds, cannon and
    /// wreck. It is a PROP test whatever its name says, which is the sort of thing canassa's names
    /// do; the set is what carries over, not the word.
    /// </remarks>
    public static bool Rises(int elementId) =>
        (elementId >= (int)TrapElementType.RedCrystal
            && elementId <= TrapPuzzleBuilder.CannonElementId)
        || elementId == CrystalChain.WreckElementId;

    /// <summary>Whether this arena plays the emergence at all: a puzzle has an exit cell.</summary>
    public static bool PlaysFor(CombatGrid grid) {
        if (grid == null) {
            return false;
        }
        for (var y = 0; y < CombatGrid.Height; y++) {
            for (var x = 0; x < CombatGrid.Width; x++) {
                if (grid.TerrainAt(x, y) == CombatTerrain.Exit) {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Bury every prop and start the clock.
    /// </summary>
    /// <param name="rnd"><c>rnd(n)</c> returns a value in <c>[0, n)</c>, as elsewhere in this tree.</param>
    /// <returns>How many props are rising; zero means there is nothing to play.</returns>
    public int Begin(TrapPuzzle puzzle, Func<int, int> rnd) {
        if (rnd == null) {
            throw new ArgumentNullException(nameof(rnd));
        }
        _restore.Clear();
        Running = false;
        _elapsed = 0f;
        if (puzzle?.Elements == null || puzzle.Grid == null || !PlaysFor(puzzle.Grid)) {
            return 0;
        }

        foreach (TrapGridElement e in puzzle.Elements) {
            if (e == null || !e.IsOnGrid || !Rises(e.ElementId)) {
                continue;
            }
            _restore.Add((e.X, e.Y, puzzle.Grid.TerrainAt(e.X, e.Y)));
            puzzle.Grid.SetTileEffect(e.X, e.Y, (CombatTerrain)RisingKind, RollDuration(rnd));
        }

        Running = _restore.Count > 0;
        return _restore.Count;
    }

    /// <summary>The per-tile duration — inclusive at both ends.</summary>
    public static int RollDuration(Func<int, int> rnd) =>
        MinimumDuration + rnd(MaximumDuration - MinimumDuration + 1);

    /// <summary>
    /// One rendered frame of the cutscene: <see cref="TicksPerFrame"/> sweeps.
    /// </summary>
    /// <returns>Whether the animation is still running afterwards.</returns>
    /// <remarks>
    /// The original's loop condition is "is any cell still kind 9", re-scanned every frame, rather
    /// than a count of what it painted — so a cell whose effect is cleared by something else ends
    /// the wait early rather than hanging it.
    /// </remarks>
    public bool AdvanceFrame(CombatGrid grid) {
        if (!Running || grid == null) {
            return false;
        }
        for (var i = 0; i < TicksPerFrame; i++) {
            grid.TickTileEffects();
        }

        Running = false;
        for (var y = 0; y < CombatGrid.Height && !Running; y++) {
            for (var x = 0; x < CombatGrid.Width; x++) {
                if ((int)grid.TerrainAt(x, y) == RisingKind) {
                    Running = true;
                    break;
                }
            }
        }

        if (!Running) {
            Finish(grid);
        }
        return Running;
    }

    /// <summary>
    /// How far a prop on this cell is still buried, in world Z units — the original draws it at
    /// <c>Z = -nEffectTimer</c>. Zero once it has surfaced, so a settled arena needs no special case.
    /// </summary>
    public int BurialAt(CombatGrid grid, int x, int y) {
        if (!Running || grid == null || (int)grid.TerrainAt(x, y) != RisingKind) {
            return 0;
        }
        int timer = grid.EffectTimerAt(x, y);
        return timer > 0 ? timer : 0;
    }

    /// <summary>
    /// Run the cutscene forward by real time, at <see cref="CutsceneFrameSeconds"/> per frame.
    /// </summary>
    /// <returns>Whether the animation is still running afterwards.</returns>
    public bool Advance(CombatGrid grid, float deltaSeconds) {
        if (!Running || grid == null) {
            return false;
        }
        _elapsed += deltaSeconds;
        while (_elapsed >= CutsceneFrameSeconds && Running) {
            _elapsed -= CutsceneFrameSeconds;
            AdvanceFrame(grid);
        }
        return Running;
    }

    /// <summary>Put the authored terrain back, so a cannon is still a cannon.</summary>
    public void Finish(CombatGrid grid) {
        Running = false;
        if (grid == null) {
            return;
        }
        foreach ((int x, int y, CombatTerrain terrain) in _restore) {
            grid.SetTileEffect(x, y, terrain, CombatGrid.NoEffect);
        }
        _restore.Clear();
    }
}
