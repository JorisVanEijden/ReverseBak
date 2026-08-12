namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>An element standing on the puzzle grid. Elements block movement wherever they stand.</summary>
public sealed class TrapGridElement {
    public TrapGridElement(int elementId, int x, int y) {
        ElementId = elementId;
        X = x;
        Y = y;
    }

    /// <summary>The original's <c>paged_id</c>: 7/8 crystals, 9/10 diamonds, 11 for every cannon.</summary>
    public int ElementId { get; }

    /// <summary>Grid position.</summary>
    public int X { get; }

    /// <inheritdoc cref="X"/>
    public int Y { get; }
}

/// <summary>A trap puzzle laid out on the combat grid.</summary>
public sealed class TrapPuzzle {
    internal TrapPuzzle(CombatGrid grid) {
        Grid = grid;
    }

    /// <summary>The grid, with terrain written and elements marked as occupied.</summary>
    public CombatGrid Grid { get; }

    /// <summary>Elements standing on the grid, in file order.</summary>
    public List<TrapGridElement> Elements { get; } = new List<TrapGridElement>();

    /// <summary>Where each party slot starts, indexed by slot; null where the file placed none.</summary>
    public (int X, int Y)?[] PartyStarts { get; } = new (int X, int Y)?[TrapPuzzleBuilder.PartySlots];

    /// <summary>
    /// False when the puzzle carried the "clear the combat flag" marker, which the original uses to
    /// say this encounter is a pure puzzle rather than a fight.
    /// </summary>
    public bool CombatFlag { get; internal set; } = true;
}

/// <summary>
/// Lays a <c>TRAPS.DAT</c> encounter out on the combat grid.
///
/// <para>Ported from <c>combatgrid_load_traps_dat</c> and the surrounding
/// <c>combatgrid_load_and_init</c> (<c>SRC/COMBAT/GRID/CMBTGRID.C</c>). The grid is the same 8×13
/// buffer combat uses — the corners and, underground, the back rows are walled off first, exactly as
/// for a fight — and the puzzle is written on top of it.</para>
///
/// <para><b>Element ids and terrain kinds are different spaces</b> and the mapping is not the
/// identity. A crystal is element 7 or 8 but writes terrain 3; a diamond is 9 or 10 but writes
/// terrain 5. Cannons write their own direction as terrain but are all recorded as element 11. Get
/// this wrong and the puzzle looks right while behaving as something else entirely.</para>
/// </summary>
public static class TrapPuzzleBuilder {
    /// <summary>Party slots a puzzle can place (markers −15, −16, −17).</summary>
    public const int PartySlots = 3;

    private const int ClearCombatFlagId = 18;
    private const int FirstPartySlotId = 15;
    private const int FirstCannonId = 10;
    private const int LastCannonId = 13;

    /// <summary>Element id every cannon is recorded as, whichever way it faces.</summary>
    public const int CannonElementId = 11;

    /// <summary>
    /// Builds the puzzle.
    /// </summary>
    /// <param name="elements">The encounter's elements, in file order.</param>
    /// <param name="underground">Walls off the back rows, as for an underground fight.</param>
    /// <param name="partySize">How many party members are present; markers beyond this are ignored,
    /// matching the original's <c>actor_idx &lt; g_combat_count_A</c> guard.</param>
    public static TrapPuzzle Build(
        IEnumerable<(int Type, int X, int Y)> elements, bool underground = false, int partySize = PartySlots) {
        var puzzle = new TrapPuzzle(new CombatGrid(underground));
        if (elements == null) {
            return puzzle;
        }

        foreach ((int type, int x, int y) in elements) {
            if (type >= 0) {
                PlacePositive(puzzle, type, x, y);
            } else {
                PlaceNegative(puzzle, -type, x, y, partySize);
            }
        }
        return puzzle;
    }

    // A positive id is an element that stands on the grid. Anything that is not a crystal or a
    // diamond is skipped outright — not placed and not counted — rather than defaulting to something.
    private static void PlacePositive(TrapPuzzle puzzle, int id, int x, int y) {
        CombatTerrain terrain;
        switch (id) {
            case 7:
            case 8:
                terrain = CombatTerrain.Crystal;
                break;
            case 9:
            case 10:
                terrain = CombatTerrain.Pushable;
                break;
            default:
                return;
        }

        puzzle.Grid.SetTerrain(x, y, terrain);
        puzzle.Grid.SetOccupied(x, y, true);
        puzzle.Elements.Add(new TrapGridElement(id, x, y));
    }

    // A negative id is a marker rather than a thing standing on the grid — except the cannons, which
    // are both.
    private static void PlaceNegative(TrapPuzzle puzzle, int id, int x, int y, int partySize) {
        if (id >= FirstCannonId && id <= LastCannonId) {
            // The terrain records which way it points; the element is recorded generically.
            puzzle.Grid.SetTerrain(x, y, (CombatTerrain)id);
            puzzle.Grid.SetOccupied(x, y, true);
            puzzle.Elements.Add(new TrapGridElement(CannonElementId, x, y));
            return;
        }

        if (id >= FirstPartySlotId && id < FirstPartySlotId + PartySlots) {
            int slot = id - FirstPartySlotId;
            // The original guards on actor_idx < g_combat_count_A: a marker for a member who is not
            // there places nobody and writes no terrain.
            if (slot < puzzle.PartyStarts.Length && slot < partySize) {
                puzzle.PartyStarts[slot] = (x, y);
                puzzle.Grid.SetTerrain(x, y, (CombatTerrain)id);
            }
            return;
        }

        if (id == ClearCombatFlagId) {
            puzzle.CombatFlag = false;
            return;
        }

        // Everything else (the exit cell, and any kind not called out above) simply writes its own
        // id as the terrain kind.
        puzzle.Grid.SetTerrain(x, y, (CombatTerrain)id);
    }
}
