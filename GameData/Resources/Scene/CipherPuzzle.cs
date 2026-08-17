namespace GameData.Resources.Scene;

using System;
using System.Collections.Generic;

/// <summary>
/// The lettered dial puzzle behind <c>REQ_PUZL</c> — <c>CIPHER.C</c>'s
/// <c>cipher_puzzle_parse_table</c> and <c>cipher_puzzle_is_solved</c>.
///
/// <para>It is a <b>combination lock made of letters</b>: every column is a wheel, each wheel shows
/// one letter from each dial row, and the puzzle is solved when the row you have selected in each
/// column spells the target word. Not free text entry — the player never types, they rotate.</para>
///
/// <para>The table is not a file of its own: it lives in the <b>tail of a DDX record</b>, after the
/// record's choices and ops, keyed <c>(puzzleId - 1) + 0x19f0a1</c>.</para>
/// </summary>
public class CipherPuzzle {
    /// <summary>Dialog-record key for a puzzle, by its 1-based id.</summary>
    public static long DialogKeyFor(int puzzleId) => (puzzleId - 1) + 0x19f0a1L;

    /// <summary>Columns in the puzzle — the length of the target word.</summary>
    public int Width => Target.Length;

    /// <summary>
    /// The word the dials must spell. <b>A space is a dead column</b>: it is skipped by the solved
    /// check and its click area is switched off, so the player cannot rotate it at all.
    /// </summary>
    public string Target { get; private set; } = string.Empty;

    /// <summary>
    /// The dial rows. Each is <see cref="Width"/> characters, and column <c>i</c> of row <c>r</c> is
    /// the letter that column shows when set to that row.
    /// </summary>
    public IReadOnlyList<string> DialRows => _dialRows;

    private readonly List<string> _dialRows = new List<string>();

    /// <summary>Offset in the source at which the puzzle's descriptive text begins.</summary>
    public int TextOffset { get; private set; }

    /// <summary>
    /// The riddle itself — everything past <see cref="TextOffset"/>.
    /// </summary>
    /// <remarks>
    /// Kept rather than left as an offset into a string the caller no longer holds: the screen has
    /// the puzzle, not the record it came out of, and re-reading the tail to find the words the
    /// player is meant to solve would mean passing the raw table around beside the parsed thing.
    /// </remarks>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Reads the table out of a DDX record's tail.
    /// </summary>
    /// <remarks>
    /// The layout, exactly as the original walks it: the target word up to a newline, then <b>three</b>
    /// bytes skipped, then dial rows of <c>width + 1</c> bytes each until a <c>#</c>, then <b>two</b>
    /// more bytes to reach the text. The odd skips are part of the format, not padding to round up.
    /// </remarks>
    public static CipherPuzzle Parse(string table) {
        if (table == null) {
            throw new ArgumentNullException(nameof(table));
        }

        var puzzle = new CipherPuzzle();
        int newline = table.IndexOf('\n');
        if (newline < 0) {
            return puzzle;
        }
        puzzle.Target = table.Substring(0, newline);

        int at = newline + 3;
        int stride = puzzle.Width + 1;
        while (at < table.Length && table[at] != '#') {
            if (at + puzzle.Width > table.Length) {
                break;
            }
            puzzle._dialRows.Add(table.Substring(at, puzzle.Width));
            at += stride;
        }
        puzzle.TextOffset = Math.Min(at + 2, table.Length);
        puzzle.Description = table.Substring(puzzle.TextOffset);

        return puzzle;
    }

    /// <summary>
    /// Whether the current wheel positions spell the target.
    /// </summary>
    /// <param name="selectedRows">
    /// The row selected in each column. Out-of-range entries are treated as unsolved rather than
    /// throwing — a fresh puzzle starts with every column on row 0.
    /// </param>
    public bool IsSolved(IReadOnlyList<int> selectedRows) {
        if (selectedRows == null) {
            return false;
        }
        for (var column = 0; column < Width; column++) {
            if (Target[column] == ' ') {
                continue; // a dead column is always satisfied
            }
            if (column >= selectedRows.Count) {
                return false;
            }
            int row = selectedRows[column];
            if (row < 0 || row >= _dialRows.Count) {
                return false;
            }
            if (_dialRows[row][column] != Target[column]) {
                return false;
            }
        }
        return true;
    }

    /// <summary>The letter a column currently shows.</summary>
    public char LetterAt(int column, int selectedRow) =>
        column >= 0 && column < Width && selectedRow >= 0 && selectedRow < _dialRows.Count
            ? _dialRows[selectedRow][column]
            : ' ';

    /// <summary>Whether a column can be rotated at all — false for the target's spaces.</summary>
    public bool IsColumnInteractive(int column) =>
        column >= 0 && column < Width && Target[column] != ' ';
}
