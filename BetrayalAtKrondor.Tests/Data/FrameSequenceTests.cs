namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Animation;
using GameData.Resources.Animation.FrameCommands;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Resolving a <c>GotoFrame</c> to the frame it names.
/// </summary>
public class FrameSequenceTests {
    [Fact]
    public void AJumpResolvesToTheFrameCARRYINGTheTag() {
        List<Frame> frames = Scene();

        Assert.Equal(2, FrameSequence.IndexOfTag(frames, 7));
    }

    [Fact]
    public void TheTAGGEDFrameStillHasItsOwnCommandsToRun() {
        // The tag is one command among the frame's others — the extractor lifts its number onto
        // Frame.Tag and leaves the contents alone. So a jump must land ON this frame; resuming
        // after it silently drops whatever the target was going to draw.
        List<Frame> frames = Scene();
        Frame target = frames[FrameSequence.IndexOfTag(frames, 7)];

        Assert.True(target.Commands.Count > 1,
            "the tagged frame carries more than its tag, so it cannot be skipped");
    }

    [Fact]
    public void ATagNothingCarriesIsNOTFOUNDRatherThanZero() {
        // Zero is a real frame index. Treating "absent" as a position restarts the scene instead
        // of reporting a broken jump.
        List<Frame> frames = Scene();

        Assert.Equal(FrameSequence.NotFound, FrameSequence.IndexOfTag(frames, 99));
        Assert.NotEqual(0, FrameSequence.IndexOfTag(frames, 99));
    }

    [Fact]
    public void TheFIRSTFrameWithTheTagWins() {
        var frames = new List<Frame> {
            new() { Tag = 4 },
            new() { Tag = 4 },
        };

        Assert.Equal(0, FrameSequence.IndexOfTag(frames, 4));
    }

    [Fact]
    public void AnUntaggedSceneAndANullOneBothAnswerNotFound() {
        Assert.Equal(FrameSequence.NotFound,
            FrameSequence.IndexOfTag(new List<Frame> { new(), new() }, 1));
        Assert.Equal(FrameSequence.NotFound, FrameSequence.IndexOfTag(null, 1));
    }

    private static List<Frame> Scene() => new() {
        new Frame { Commands = { new StoreScreen() } },
        new Frame { Commands = { new StoreScreen() } },
        new Frame { Tag = 7, Commands = { new TagFrame(), new StoreScreen() } },
        new Frame { Commands = { new StoreScreen() } },
    };
}
