namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using System;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The trap arena's props rising out of the ground as the fight opens.
/// </summary>
/// <remarks>
/// Built against a measurement of the running original rather than from the C alone: 140 consecutive
/// frames of encounter 347 (2026-09-06) showed a linear rise, no easing, and — the part that decides
/// the design — the two nearest props settling SIX FRAMES APART. The cases below pin the per-tile
/// roll that causes that, and the terrain restore without which a cannon comes out of the animation
/// pointing north.
/// </remarks>
public class TrapPropEmergenceTests {
    // Encounter 347's own shape, trimmed: two red crystals, two north cannons, an exit.
    private static List<(int, int, int)> Encounter347Like() => new() {
        (7, 0, 1),      // red crystal
        (7, 7, 1),      // red crystal
        (-12, 2, 2),    // terrain: cannon north
        (-12, 6, 2),    // terrain: cannon north
        (-6, 7, 5),     // terrain: exit — the gate for the whole cutscene
    };

    private static TrapPuzzle Puzzle() => TrapPuzzleBuilder.Build(Encounter347Like());

    /// <summary>A deterministic stand-in for rnd(n) in [0, n).</summary>
    private static Func<int, int> Rolls(params int[] values) {
        var i = 0;
        return _ => values[i++ % values.Length];
    }

    [Fact]
    public void ItPlaysOnlyForAPuzzleArena_WhichIsWhatAnExitCellMeans() {
        Assert.True(TrapPropEmergence.PlaysFor(Puzzle().Grid));

        // The same encounter without its exit is an ordinary fight, and ordinary fights do not
        // play this. The original asks exactly this question (combatgrid_any_terrain_6).
        TrapPuzzle noExit = TrapPuzzleBuilder.Build(new List<(int, int, int)> { (7, 0, 1) });
        Assert.False(TrapPropEmergence.PlaysFor(noExit.Grid));
    }

    [Fact]
    public void EveryPropGetsItsOwnDuration_WhichIsWhyTheyLandAtDifferentTimes() {
        TrapPuzzle puzzle = Puzzle();
        var emergence = new TrapPropEmergence();

        // Two different rolls, handed out in turn: the props must not share a clock.
        int placed = emergence.Begin(puzzle, Rolls(0, 299));
        Assert.Equal(4, placed);   // two crystals, two cannons; the exit is not a prop

        var timers = new List<int>();
        foreach ((int x, int y) in new[] { (0, 1), (7, 1), (2, 2), (6, 2) }) {
            timers.Add(puzzle.Grid.EffectTimerAt(x, y));
        }
        Assert.Contains(TrapPropEmergence.MinimumDuration, timers);
        Assert.Contains(TrapPropEmergence.MaximumDuration, timers);
    }

    [Fact]
    public void TheDurationIsInclusiveAtBothEnds() {
        // RNDR(lo,hi) is lo + rand()%(hi-lo+1), so hi is reachable. An exclusive port would make the
        // longest emergence one tick short and would never be noticed.
        Assert.Equal(TrapPropEmergence.MinimumDuration, TrapPropEmergence.RollDuration(_ => 0));
        Assert.Equal(TrapPropEmergence.MaximumDuration,
            TrapPropEmergence.RollDuration(n => n - 1));
        Assert.Equal(0x190, TrapPropEmergence.MinimumDuration);
        Assert.Equal(0x2bb, TrapPropEmergence.MaximumDuration);
    }

    [Fact]
    public void APropIsBuriedByItsTimerAndSurfacesLinearly() {
        TrapPuzzle puzzle = Puzzle();
        var emergence = new TrapPropEmergence();
        emergence.Begin(puzzle, Rolls(0));   // every prop takes the minimum

        int first = emergence.BurialAt(puzzle.Grid, 0, 1);
        Assert.Equal(TrapPropEmergence.MinimumDuration, first);

        emergence.AdvanceFrame(puzzle.Grid);
        int second = emergence.BurialAt(puzzle.Grid, 0, 1);

        // One frame is exactly TicksPerFrame closer to the surface — the rate the film showed as a
        // constant ~4 screen px per frame.
        Assert.Equal(first - TrapPropEmergence.TicksPerFrame, second);
    }

    [Fact]
    public void ItRunsUntilTheLastPropIsUp_NotUntilTheFirst() {
        TrapPuzzle puzzle = Puzzle();
        var emergence = new TrapPropEmergence();
        emergence.Begin(puzzle, Rolls(0, 299));   // shortest and longest, alternating

        var frames = 0;
        while (emergence.AdvanceFrame(puzzle.Grid) && frames < 1000) {
            frames++;
        }

        Assert.False(emergence.Running);
        // The longest roll decides the length: 699 ticks at 15 a frame.
        int expected = (int)Math.Ceiling(
            (TrapPropEmergence.MaximumDuration + 1.0) / TrapPropEmergence.TicksPerFrame);
        Assert.InRange(frames, expected - 2, expected + 1);

        // And everything is up.
        Assert.Equal(0, emergence.BurialAt(puzzle.Grid, 0, 1));
        Assert.Equal(0, emergence.BurialAt(puzzle.Grid, 2, 2));
    }

    [Fact]
    public void TheTerrainGoesBACK_OrEveryCannonAimsNorthForever() {
        TrapPuzzle puzzle = Puzzle();
        CombatTerrain cannonBefore = puzzle.Grid.TerrainAt(2, 2);
        CombatTerrain crystalBefore = puzzle.Grid.TerrainAt(0, 1);
        Assert.Equal(CombatTerrain.CannonNorth, cannonBefore);

        var emergence = new TrapPropEmergence();
        emergence.Begin(puzzle, Rolls(0));
        while (emergence.AdvanceFrame(puzzle.Grid)) { }

        // *** THE POINT OF THE BACKUP. *** Painting the effect overwrites the cell's kind, and the
        // tick's expiry rule turns a 9 into Crystal — so without the restore a cannon cell comes out
        // as crystal ground and CannonFacing reads the wrong direction for the rest of the fight.
        Assert.Equal(cannonBefore, puzzle.Grid.TerrainAt(2, 2));
        Assert.Equal(crystalBefore, puzzle.Grid.TerrainAt(0, 1));
        Assert.Equal(CombatGrid.NoEffect, puzzle.Grid.EffectTimerAt(2, 2));
    }

    [Fact]
    public void OnlyPropsRise() {
        // The original's predicate is named combatgrid_is_combatant_type and selects props; the set
        // is what carries over, not the word.
        Assert.True(TrapPropEmergence.Rises((int)TrapElementType.RedCrystal));
        Assert.True(TrapPropEmergence.Rises((int)TrapElementType.GreenCrystal));
        Assert.True(TrapPropEmergence.Rises((int)TrapElementType.DiamondSolid));
        Assert.True(TrapPropEmergence.Rises((int)TrapElementType.DiamondPassthrough));
        Assert.True(TrapPropEmergence.Rises(TrapPuzzleBuilder.CannonElementId));
        Assert.True(TrapPropEmergence.Rises(CrystalChain.WreckElementId));

        Assert.False(TrapPropEmergence.Rises((int)TrapElementType.Empty));
        Assert.False(TrapPropEmergence.Rises(6));    // the exit marker is terrain, not a prop
    }

    [Fact]
    public void NothingRisesWithoutAnExit_SoAnOrdinaryFightIsUntouched() {
        TrapPuzzle plain = TrapPuzzleBuilder.Build(new List<(int, int, int)> { (7, 3, 3) });
        var emergence = new TrapPropEmergence();

        Assert.Equal(0, emergence.Begin(plain, Rolls(0)));
        Assert.False(emergence.Running);
        // And a settled arena reports no burial, so the renderer needs no special case for it.
        Assert.Equal(0, emergence.BurialAt(plain.Grid, 3, 3));
    }
}
