namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;

using System.Collections.Generic;
using System.Linq;

using Xunit;

/// <summary>
/// DDX inline formatting — <c>drawCharacter</c> @0x15eef-0x15fa5.
/// </summary>
public class DialogTextRunsTests {
    private const int BodyPen = 0;

    private static List<DialogTextRuns.Run> Decode(string text, int bodyPen = BodyPen) =>
        DialogTextRuns.Decode(text, 0, text.Length, bodyPen);

    private static string Slice(string text, DialogTextRuns.Run r) =>
        text.Substring(r.Start, r.Length);

    [Fact]
    public void PlainTextIsOneUnstyledRun() {
        const string text = "hello there";
        List<DialogTextRuns.Run> runs = Decode(text);

        Assert.Single(runs);
        Assert.Equal(text, Slice(text, runs[0]));
        Assert.False(runs[0].Italic);
        Assert.Equal(BodyPen, runs[0].Pen);
    }

    [Fact]
    public void ControlCodesAreNeverPartOfARun() {
        const string text = "a≤bc";
        List<DialogTextRuns.Run> runs = Decode(text);

        // The codes are text bytes in the source; emitting one would print a stray CP437 glyph.
        Assert.DoesNotContain(runs, r => Slice(text, r).Any(DialogTextRuns.IsControlCode));
        Assert.Equal("abc", string.Concat(runs.Select(r => Slice(text, r))));
    }

    [Fact]
    public void AHighlightGivesTheBlackBodiedDialogPenFive() {
        // The cream/tan highlight on the chapter-intro title. Pen 0 is the common body pen.
        List<DialogTextRuns.Run> runs = Decode("±Dark");

        Assert.Single(runs);
        Assert.True(runs[0].Italic);
        Assert.Equal(5, runs[0].Pen);
    }

    [Fact]
    public void TheTwoHighlightCodesAreInterchangeable() {
        Assert.Equal(Decode("±x")[0].Pen, Decode("≥x")[0].Pen);
        Assert.Equal(Decode("±x")[0].Italic, Decode("≥x")[0].Italic);
    }

    [Fact]
    public void PlainItalicLeavesThePenAlone() {
        List<DialogTextRuns.Run> runs = Decode("≤x", bodyPen: 0x0B);

        Assert.True(runs[0].Italic);
        Assert.Equal(0x0B, runs[0].Pen);
    }

    [Fact]
    public void ASpaceResetsBothItalicAndPen() {
        // This is what makes styling one wrapped LINE at a time equivalent to styling the block:
        // no style can survive a break.
        const string text = "±one two";
        List<DialogTextRuns.Run> runs = Decode(text);

        Assert.Equal(2, runs.Count);
        Assert.Equal("one", Slice(text, runs[0]));
        Assert.True(runs[0].Italic);
        Assert.Equal(" two", Slice(text, runs[1]));
        Assert.False(runs[1].Italic);
        Assert.Equal(BodyPen, runs[1].Pen);
    }

    [Fact]
    public void ResetEndsAStyledRunMidWord() {
        const string text = "±ab≡cd";
        List<DialogTextRuns.Run> runs = Decode(text);

        Assert.Equal(2, runs.Count);
        Assert.True(runs[0].Italic);
        Assert.Equal("ab", Slice(text, runs[0]));
        Assert.False(runs[1].Italic);
        Assert.Equal("cd", Slice(text, runs[1]));
    }

    [Fact]
    public void TheDoubleRemapIsNotTheSingleOneTwice_ItHasItsOwnFirstStep() {
        // 0xF4 falls THROUGH into 0xF5, so it applies an extra first step first. From pen 0 the
        // single remap gives 1; the double gives 0 -> 0x0A -> 0. Treating them as one code, or
        // applying the same step twice, both produce a different colour.
        Assert.Equal(1, Decode("⌡x")[0].Pen);
        Assert.Equal(0, Decode("⌠x")[0].Pen);
    }

    [Fact]
    public void TheRemapDependsOnTheCurrentPen_NotOnlyOnTheCode() {
        // Why a control code cannot be interpreted in isolation.
        Assert.Equal(1, Decode("⌡x", bodyPen: 0)[0].Pen);
        Assert.Equal(0x0B, Decode("⌡x", bodyPen: 1)[0].Pen);
        Assert.Equal(0, Decode("⌡x", bodyPen: 0x0A)[0].Pen);
    }

    [Fact]
    public void ARangeIsDecodedFromTheDefaultState() {
        // What lets the wrap run before styling: each line starts fresh, as drawTextString does.
        const string text = "±one two";
        // Index 3 is the 'e' of "one" — inside the italic run in the full string, but decoded on
        // its own it starts from the default state, exactly as a wrapped line does.
        List<DialogTextRuns.Run> runs = DialogTextRuns.Decode(text, 3, text.Length, BodyPen);

        Assert.Single(runs);
        Assert.Equal("e two", Slice(text, runs[0]));
        Assert.False(runs[0].Italic);
    }
}
