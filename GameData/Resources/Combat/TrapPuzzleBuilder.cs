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
    public int X { get; internal set; }

    /// <inheritdoc cref="X"/>
    public int Y { get; internal set; }

    /// <summary>
    /// False once the element has left the board. The original marks this by writing 0xff to both
    /// tile coordinates rather than removing the entry, so indices stay stable.
    /// </summary>
    public bool IsOnGrid => X != OffGrid && Y != OffGrid;

    /// <summary>The original's off-grid marker.</summary>
    public const int OffGrid = 0xff;

    internal void RemoveFromGrid() {
        X = OffGrid;
        Y = OffGrid;
    }
}

/// <summary>How a push attempt ended.</summary>
public enum PushResult {
    /// <summary>The destination is blocked; the element does not move and the step is refused.</summary>
    Blocked,

    /// <summary>Nothing stood on the source tile.</summary>
    NoElement,

    /// <summary>The element moved one tile.</summary>
    Moved,

    /// <summary>
    /// The element was pushed onto crystal ground: it is destroyed and the crystal goes off. This is
    /// how a trap puzzle is solved.
    /// </summary>
    CrystalFired,
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
    /// False when the encounter carried the <see cref="TrapElementType.RetreatLock"/> marker: the
    /// party may not retreat from this fight.
    /// </summary>
    /// <remarks>
    /// <b>Corrected 2026-08-24 — this was recorded as "a pure puzzle rather than a fight", which was
    /// a guess and is wrong.</b> The flag has exactly one behavioural consumer, the escape roll in
    /// <see cref="CombatCommands.EscapeRollPasses"/>; its only other reads are in the original's
    /// TRAPS.DAT writer, which round-trips the marker back to disk as <c>0xffee</c>. Nothing
    /// anywhere branches on it to decide whether an encounter is a fight.
    ///
    /// <para><b>The default is the meaningful half.</b> The original raises this flag before reading
    /// the record and only the marker lowers it, so it is an opt-OUT that five of 768 encounters
    /// use — see <see cref="TrapElementType.RetreatLock"/>.</para>
    /// </remarks>
    public bool AllowsRetreat { get; internal set; } = true;

    /// <summary>
    /// Shoves whatever stands on a tile one step along <paramref name="dx"/>,<paramref name="dy"/>.
    /// </summary>
    /// <remarks>
    /// Ported from <c>combatgrid_place_actor_on_tile</c>. The source tile is cleared to open ground
    /// whatever happens, and then:
    /// <list type="bullet">
    ///   <item>onto ordinary ground — the element moves and the tile becomes pushable in its turn;</item>
    ///   <item><b>onto crystal ground — the element is destroyed and the crystal goes off.</b> That
    ///   is the puzzle: you solve a room by shoving diamonds into crystals rather than by walking
    ///   anywhere.</item>
    /// </list>
    /// <para>Note the crystal case is only reachable once the crystal's own element has gone, because
    /// any element blocks and a blocked destination refuses the push. The terrain outlives the
    /// element, which is what leaves the ground armed.</para>
    /// <para>The caller runs the consequences of <see cref="PushResult.CrystalFired"/> — the line
    /// spreads along the crystal run and damages what it reaches. Not modelled here yet.</para>
    /// </remarks>
    public PushResult TryPush(int fromX, int fromY, int dx, int dy) {
        int toX = fromX + dx;
        int toY = fromY + dy;

        if (Grid.IsBlocked(toX, toY)) {
            return PushResult.Blocked;
        }

        TrapGridElement element = ElementAt(fromX, fromY);
        if (element == null) {
            return PushResult.NoElement;
        }

        Grid.SetTerrain(fromX, fromY, CombatTerrain.Open);
        Grid.SetOccupied(fromX, fromY, false);

        if (Grid.TerrainAt(toX, toY) == CombatTerrain.Crystal) {
            element.RemoveFromGrid();
            return PushResult.CrystalFired;
        }

        element.X = toX;
        element.Y = toY;
        Grid.SetTerrain(toX, toY, CombatTerrain.Pushable);
        Grid.SetOccupied(toX, toY, true);
        return PushResult.Moved;
    }

    /// <summary>
    /// The run of crystal tiles a firing crystal sweeps, in the order the effect travels.
    /// </summary>
    /// <remarks>
    /// <para>The original does this as two passes over the same walk: <c>crystalRun_markFiring</c>
    /// @0x2ee5a rewrites the run's terrain from crystal (3) to firing (4), plays a short cine, then
    /// <c>crystalRun_clearFiring</c> @0x2ef2c walks it again rewriting 4 back to 3. <b>The flip nets
    /// to nothing for any RULE</b> — nothing that decides anything reads 4 — so the useful thing to
    /// port is the run itself, which is what this returns.
    ///
    /// <para><b>But something does read it, and this used to say nothing did.</b>
    /// <c>grid_drawTileFeatureEdge</c> @0x30f4e is the only reader of element 4 in the binary, and
    /// it outlines the run: it probes the cells before and after along the run's axis and emits an
    /// edge exactly where a 4 meets a non-4. The flip IS the visual. A port that treats it as a
    /// no-op has dropped the thing it exists for, which is a different mistake from animating it
    /// differently.</para>
    ///
    /// <para><b>And the lit stretch is not the whole run.</b> Both passes back up to the run's end,
    /// then advance to the first cell holding a trap ELEMENT, and only stamp from there — so the
    /// light covers the run from its first object onward, not end to end.</para></para>
    ///
    /// <para><b>The sweep does no damage.</b> The only other thing the push does is
    /// <c>apply_tile_status_fx</c>, which builds a throwaway actor purely to play a sound and a
    /// particle burst. The 100 damage associated with crystals belongs to a different path entirely
    /// — a party member <i>walking onto</i> crystal ground, applied by the movement loop, not a
    /// diamond being shoved in.</para>
    ///
    /// <para>The walk is: find the axis the run lies on, back up to its start, advance to the first
    /// tile holding an element, then collect consecutive crystal tiles from there.</para>
    /// </remarks>
    public IReadOnlyList<(int X, int Y)> TraceCrystalLine(int x, int y) {
        var run = new List<(int X, int Y)>();
        (int dx, int dy) = FindLineDirection(x, y);

        while (CombatGrid.InBounds(x, y) && Grid.TerrainAt(x - dx, y - dy) == CombatTerrain.Crystal) {
            x -= dx;
            y -= dy;
        }
        while (CombatGrid.InBounds(x, y) && ElementAt(x, y) == null) {
            x += dx;
            y += dy;
        }
        while (CombatGrid.InBounds(x, y) && Grid.TerrainAt(x, y) == CombatTerrain.Crystal) {
            run.Add((x, y));
            x += dx;
            y += dy;
        }
        return run;
    }

    /// <summary>
    /// Which way the crystal run lies, as a neighbour offset. <b>Never (0,0).</b>
    /// </summary>
    /// <remarks>
    /// Two cases, and they differ in what they will accept as the next tile. From a crystal that is
    /// still standing, the run continues through a crystal tile that is <b>not</b> holding another
    /// crystal. From anywhere else it continues through one that is empty or holds a crystal, and
    /// only if the run also carries on behind you — or something is standing where you are. The
    /// neighbours are tried in the original's order, which starts at the top-left and reads down,
    /// and the FIRST match wins — so the answer is order-dependent, not "the best" axis.
    ///
    /// <para><b>THE ORIGINAL NEVER ANSWERS "NO AXIS", AND THIS USED TO.</b>
    /// <c>grid_findRunAxis</c> @0x2ec96 falls back to <b>(0,1)</b> — vertical — unless BOTH
    /// horizontal neighbours hold a trap element, in which case (1,0). Its return value is not a
    /// found flag and a caller cannot tell a real answer from the default. Returning (0,0) here made
    /// a lone crystal sweep nothing where the original sweeps its own tile, and a test was pinning
    /// that shortcut.</para>
    ///
    /// <para><b>The element is hardcoded to <see cref="CombatTerrain.Crystal"/> and the original
    /// takes it as an argument.</b> Its other two callers pass element 4, the firing state — the
    /// restore pass and the outline renderer both trace a run of 4s. We do not model that state, so
    /// nothing needs the parameter yet; whoever ports the visual sweep will.</para>
    /// </remarks>
    private (int Dx, int Dy) FindLineDirection(int x, int y) {
        TrapGridElement here = ElementAt(x, y);
        bool fromCrystal = here != null && IsCrystalElement(here.ElementId);

        for (var dx = -1; dx <= 1; dx++) {
            for (var dy = -1; dy <= 1; dy++) {
                if (dx == 0 && dy == 0) {
                    continue;
                }
                if (Grid.TerrainAt(x + dx, y + dy) != CombatTerrain.Crystal) {
                    continue;
                }

                TrapGridElement neighbour = ElementAt(x + dx, y + dy);
                bool neighbourIsCrystal = neighbour != null && IsCrystalElement(neighbour.ElementId);

                if (fromCrystal) {
                    if (!neighbourIsCrystal) {
                        return (dx, dy);
                    }
                } else if (neighbour == null || neighbourIsCrystal) {
                    bool runsOnBehind = Grid.TerrainAt(x - dx, y - dy) == CombatTerrain.Crystal;
                    if (runsOnBehind || here != null) {
                        return (dx, dy);
                    }
                }
            }
        }

        // The fallback, straight from grid_findRunAxis: vertical, unless the cell is flanked
        // left and right by trap elements.
        return ElementAt(x + 1, y) != null && ElementAt(x - 1, y) != null ? (1, 0) : (0, 1);
    }

    private static bool IsCrystalElement(int elementId) => elementId == 7 || elementId == 8;

    /// <summary>
    /// The noise a fired crystal makes — <c>sound_arrowexp</c>, <c>0x1d</c>.
    /// </summary>
    /// <remarks>
    /// <b>The whole consequence of shoving a diamond into a crystal is a sound, a particle burst
    /// and a run that lights up.</b> <c>combatGrid_playTileFx</c> @0x2e9e3 stands a synthetic actor
    /// on the tile purely so the effect machinery has something to act on, plays this, and bursts
    /// 25 particles in colour 111. It also calls <c>ApplySpellToActor(phantom, Flamecast, cost 0)</c>
    /// — <b>on the phantom</b>, which is freed with the stack frame, so reading that as "the crystal
    /// damages what it hits" would invent a rule. The 100 damage tied to crystals is a different
    /// path: an actor <i>walking onto</i> crystal ground.
    ///
    /// <para>The run flip either side of it (<c>crystalRun_markFiring</c> /
    /// <c>_clearFiring</c>) is what <see cref="TraceCrystalLine"/> returns the run for.</para>
    /// </remarks>
    public const int FiredSoundId = 0x1d;

    /// <summary>The element standing on a tile, or null.</summary>
    public TrapGridElement ElementAt(int x, int y) {
        foreach (TrapGridElement e in Elements) {
            if (e.IsOnGrid && e.X == x && e.Y == y) {
                return e;
            }
        }
        return null;
    }
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

    private const int RetreatLockId = -(int)TrapElementType.RetreatLock;
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

        LinkCrystals(puzzle);
        return puzzle;
    }

    /// <summary>
    /// Lay the crystal ground that joins every aligned pair of crystals.
    /// </summary>
    /// <remarks>
    /// <b>TRAPS.DAT stores the crystals and NOT the ground between them.</b>
    /// <c>Load_traps.dat</c> calls <c>crystalChain_paintLinksFromIndex</c> @0x2f707, which walks
    /// every pair and hands the aligned ones to <c>crystalChain_linkCells</c> @0x2ea83 to paint.
    /// This pass was missing entirely, so <b>every crystal was isolated by construction</b> — and
    /// crystal ground is what <see cref="CrystalChain.TileCarriesRun"/>,
    /// <see cref="CrystalChain.RunContinues"/>, <see cref="CrystalChain.IsolationDestroys"/> and
    /// <see cref="TraceCrystalLine"/> all read. Four correct rules, all answering wrongly.
    ///
    /// <para><b>The endpoints are not painted here</b>, and that is the original's shape rather than
    /// an optimisation: its walk advances before it paints, so the ends keep whatever
    /// <see cref="PlacePositive"/> gave them. Painting them again would be harmless today and wrong
    /// the moment the two differ.</para>
    ///
    /// <para><b>The run overwrites what it crosses.</b> The original applies the crystal element to
    /// every cell on the line unconditionally, so a diamond parked between two aligned crystals has
    /// its terrain replaced. Kept, because a port that skipped occupied cells would leave a hole in
    /// a run the game paints solid.</para>
    /// </remarks>
    private static void LinkCrystals(TrapPuzzle puzzle) {
        for (var a = 0; a < puzzle.Elements.Count; a++) {
            for (var b = a + 1; b < puzzle.Elements.Count; b++) {
                // The original does not restrict b > a and paints each run twice; the paint is
                // idempotent, so half the pairs give the same grid.
                if (Aligned(puzzle.Elements[a], puzzle.Elements[b])) {
                    PaintRunBetween(puzzle, puzzle.Elements[a], puzzle.Elements[b]);
                }
            }
        }
    }

    /// <summary>
    /// Whether two elements are a linked pair — <c>crystalTrapsAligned</c> @0x2f5ec.
    /// </summary>
    /// <remarks>
    /// Three conditions, all required: the <b>same</b> element id, <b>both</b> crystals, and a
    /// straight or perfectly diagonal offset. The same-id test is not redundant with the crystal
    /// test — a red crystal and a green one are both crystals and are <b>not</b> linked.
    /// </remarks>
    private static bool Aligned(TrapGridElement a, TrapGridElement b) =>
        a.ElementId == b.ElementId
        && CrystalChain.IsCrystalElement(a.ElementId)
        && CrystalChain.IsCrystalElement(b.ElementId)
        && (a.X == b.X || a.Y == b.Y
            || System.Math.Abs(a.X - b.X) == System.Math.Abs(a.Y - b.Y));

    private static void PaintRunBetween(TrapPuzzle puzzle, TrapGridElement from, TrapGridElement to) {
        int dx = System.Math.Sign(to.X - from.X);
        int dy = System.Math.Sign(to.Y - from.Y);
        if (dx == 0 && dy == 0) {
            return;   // two elements on one cell: the original's walk would never terminate
        }

        for (int x = from.X + dx, y = from.Y + dy; x != to.X || y != to.Y; x += dx, y += dy) {
            puzzle.Grid.SetTerrain(x, y, CombatTerrain.Crystal);
        }
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

        if (id == RetreatLockId) {
            puzzle.AllowsRetreat = false;
            return;
        }

        // Everything else (the exit cell, and any kind not called out above) simply writes its own
        // id as the terrain kind.
        puzzle.Grid.SetTerrain(x, y, (CombatTerrain)id);
    }
}
