namespace BetrayalAtKrondor.Tests.Scene;

using GameData.Resources.Scene;
using Xunit;

/// <summary>
/// The REQ_PUZL dial puzzle. It is a combination lock made of letters, not text entry — the player
/// rotates each column's wheel until the selected rows spell the target.
/// </summary>
public class CipherPuzzleTests {
    // Target "CAT", three dial rows. Column 0 spells C on row 2, column 1 A on row 0,
    // column 2 T on row 1 — so the solution is {2, 0, 1}.
    private const string Table = "CAT\n" + "xx" + "AAX\nBBT\nCCZ\n" + "#" + "\0" + "the riddle text";

    private static CipherPuzzle Cat() => CipherPuzzle.Parse(Table);

    [Fact]
    public void TheTargetIsTheFirstLineAndSetsTheWidth() {
        CipherPuzzle puzzle = Cat();

        Assert.Equal("CAT", puzzle.Target);
        Assert.Equal(3, puzzle.Width);
    }

    [Fact]
    public void TheDialRowsFollowAfterTheThreeByteSkip() {
        CipherPuzzle puzzle = Cat();

        Assert.Equal(new[] { "AAX", "BBT", "CCZ" }, puzzle.DialRows);
    }

    [Fact]
    public void TheDescriptiveTextStartsAfterTheHash() {
        CipherPuzzle puzzle = Cat();

        Assert.Equal("the riddle text", Table.Substring(puzzle.TextOffset));
    }

    [Fact]
    public void TheRightCombinationSolvesIt() {
        Assert.True(Cat().IsSolved(new[] { 2, 0, 1 }));
    }

    [Fact]
    public void AnyWrongWheelLeavesItUnsolved() {
        CipherPuzzle puzzle = Cat();

        Assert.False(puzzle.IsSolved(new[] { 0, 0, 1 }));
        Assert.False(puzzle.IsSolved(new[] { 2, 1, 1 }));
        Assert.False(puzzle.IsSolved(new[] { 2, 0, 0 }));
    }

    [Fact]
    public void AFreshPuzzleStartsUnsolved() {
        // Every column on row 0 is the starting state.
        Assert.False(Cat().IsSolved(new[] { 0, 0, 0 }));
    }

    [Fact]
    public void ASpaceInTheTargetIsADeadColumn() {
        // It is skipped by the check and cannot be rotated — the original switches its click area
        // off entirely rather than letting the player spin a wheel that means nothing.
        CipherPuzzle puzzle = CipherPuzzle.Parse("A B\n" + "xx" + "AZB\nQZQ\n#" + "\0" + "text");

        Assert.False(puzzle.IsColumnInteractive(1));
        Assert.True(puzzle.IsColumnInteractive(0));
        Assert.True(puzzle.IsColumnInteractive(2));

        // The middle wheel's position is irrelevant to the answer.
        Assert.True(puzzle.IsSolved(new[] { 0, 0, 0 }));
        Assert.True(puzzle.IsSolved(new[] { 0, 1, 0 }));
    }

    [Fact]
    public void AColumnReportsTheLetterItCurrentlyShows() {
        CipherPuzzle puzzle = Cat();

        Assert.Equal('A', puzzle.LetterAt(0, 0));
        Assert.Equal('C', puzzle.LetterAt(0, 2));
        Assert.Equal('T', puzzle.LetterAt(2, 1));
    }

    [Fact]
    public void OutOfRangeSelectionsAreUnsolvedRatherThanThrowing() {
        CipherPuzzle puzzle = Cat();

        Assert.False(puzzle.IsSolved(new[] { 2, 0 }));      // too few columns
        Assert.False(puzzle.IsSolved(new[] { 9, 0, 1 }));   // no such row
        Assert.False(puzzle.IsSolved(new[] { -1, 0, 1 }));
        Assert.False(puzzle.IsSolved(null));
    }

    [Fact]
    public void ThePuzzlesTextComesFromADialogRecordNotItsOwnFile() {
        // (puzzleId - 1) + 0x19f0a1
        Assert.Equal(0x19f0a1L, CipherPuzzle.DialogKeyFor(1));
        Assert.Equal(0x19f0a5L, CipherPuzzle.DialogKeyFor(5));
    }

    [Fact]
    public void ATableWithNoNewlineYieldsAnEmptyPuzzleRatherThanThrowing() {
        CipherPuzzle puzzle = CipherPuzzle.Parse("nonsense");

        Assert.Equal(0, puzzle.Width);
        Assert.Empty(puzzle.DialRows);
    }

    [Fact]
    public void TheRIDDLEItselfIsKept() {
        // The screen holds the puzzle, not the record it came out of, so the words the player is
        // meant to solve have to survive the parse.
        CipherPuzzle p = CipherPuzzle.Parse(
            "FIRE\n#\nWSTE\nAINL\nFORN\nBREH\n#\nThe chill of its death,\nYou may soon mourn.");

        Assert.StartsWith("The chill of its death", p.Description);
        Assert.Contains("soon mourn", p.Description);
    }

    [Fact]
    public void APuzzleWithNoTrailingTextHasAnEmptyRiddleRatherThanThrowing() {
        CipherPuzzle p = CipherPuzzle.Parse("FIRE\n#\nWSTE\nAINL\nFORN\nBREH\n#\n");

        Assert.NotNull(p.Description);
    }
}
