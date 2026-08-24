namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The per-encounter retreat lock — TRAPS.DAT element type -18, read through the puzzle the
/// encounter builds.
/// </summary>
public class TrapRetreatLockTests {
    /// <summary>
    /// An encounter carrying the given element types, each on its OWN tile.
    /// </summary>
    /// <remarks>
    /// <b>Distinct tiles matter.</b> Stacking every element on one tile lets a later marker overwrite
    /// an earlier one's terrain — an exit followed by two actor markers leaves no exit at all — and
    /// tile (0,0) is a corner the grid walls off anyway. A helper that stacked them made a test fail
    /// for a reason that had nothing to do with what it was asserting.
    /// </remarks>
    private static TrapData WithElements(int encounterNumber, params int[] types) {
        var data = new TrapData("TRAPS.DAT");
        var record = new TrapEncounter { Index = encounterNumber };
        var column = 1;
        foreach (int type in types) {
            record.Elements.Add(new TrapElement { Type = type, GridX = column++, GridY = 4 });
        }
        data.Encounters.Add(record);
        return data;
    }

    private static bool AllowsRetreat(TrapData data, int encounterNumber) =>
        TrapPuzzleBuilder.Build(data.ElementsFor(encounterNumber)).AllowsRetreat;

    [Fact]
    public void TheLockIsAnOptOutSoAnOrdinaryEncounterAllowsRetreat() {
        // The polarity is the whole point: the original raises the flag before reading the record,
        // so "has a record but no lock element" must answer the same as "has no record at all".
        TrapData data = WithElements(12, (int)TrapElementType.RedCrystal,
            (int)TrapElementType.ActorSlot0);
        Assert.True(AllowsRetreat(data, 12));
        Assert.True(AllowsRetreat(data, 999));
    }

    [Fact]
    public void TheLockElementForbidsIt() {
        TrapData data = WithElements(12, (int)TrapElementType.RedCrystal,
            (int)TrapElementType.RetreatLock);
        Assert.False(AllowsRetreat(data, 12));
    }

    [Fact]
    public void AnExitTileIsNotTheLock() {
        // 35 encounters carry an exit and only 5 carry the lock. Deriving "no retreat" from the exit
        // would lock thirty fights the game lets you leave, which is why these are asserted apart.
        TrapData data = WithElements(12, (int)TrapElementType.Exit,
            (int)TrapElementType.ActorSlot0, (int)TrapElementType.ActorSlot1);
        Assert.True(AllowsRetreat(data, 12));
        Assert.True(TrapPuzzleGoal.IsTrapPuzzle(
            TrapPuzzleBuilder.Build(data.ElementsFor(12)).Grid),
            "it IS a puzzle — that is the exit's other job, and a different question");
    }

    [Fact]
    public void ElementsSurviveTheTripToTheBuilderWithTheirTilesIntact() {
        // The adapter is the only path from the file to the rules, so a transposed X/Y here would
        // silently move every crystal in the game.
        var data = new TrapData("TRAPS.DAT");
        var record = new TrapEncounter { Index = 3 };
        record.Elements.Add(new TrapElement {
            Type = (int)TrapElementType.RedCrystal, GridX = 2, GridY = 5,
        });
        data.Encounters.Add(record);

        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(data.ElementsFor(3));

        Assert.Equal(CombatTerrain.Crystal, puzzle.Grid.TerrainAt(2, 5));
    }
}
