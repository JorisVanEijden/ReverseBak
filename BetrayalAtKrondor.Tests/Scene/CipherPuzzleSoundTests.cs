namespace BetrayalAtKrondor.Tests.Scene;

using System.Linq;
using GameData.Resources.Scene;
using Xunit;

/// <summary>
/// The two-bolt sequence a solved cipher puzzle plays.
/// </summary>
public class CipherPuzzleSoundTests {
    [Fact]
    public void TheSequenceHasTwoBolts() {
        Assert.Equal(CipherPuzzleSound.Bolts, CipherPuzzleSound.BoltDelaysSeconds.Count());
    }

    /// <summary>The mechanism starts slowly and finishes quickly.</summary>
    /// <remarks>
    /// Pinned as an ORDERING rather than as two numbers: the delays are RE-derived tick counts and
    /// could be recalibrated, but a port that made them equal — or swapped them — would lose the
    /// shape of the sound without failing anything.
    /// </remarks>
    [Fact]
    public void TheSecondBoltFollowsFasterThanTheFirst() {
        double[] delays = CipherPuzzleSound.BoltDelaysSeconds.ToArray();
        Assert.True(delays[1] < delays[0]);
    }

    /// <summary>Both waits are real — a zero would collapse the sequence into one noise.</summary>
    [Fact]
    public void EveryBoltWaits() {
        Assert.All(CipherPuzzleSound.BoltDelaysSeconds, d => Assert.True(d > 0));
    }

    /// <summary>The whole sequence is seconds, not a frame and not a minute.</summary>
    /// <remarks>
    /// A sanity bound on the tick conversion: reading the counts as milliseconds or as frames at
    /// 60fps would put the total an order of magnitude out in either direction, and nothing else
    /// here would notice.
    /// </remarks>
    [Fact]
    public void TheSequenceLastsAFewSeconds() {
        double total = CipherPuzzleSound.BoltDelaysSeconds.Sum();
        Assert.InRange(total, 2.0, 6.0);
    }

    /// <summary>Every bolt has a latch to paint, and there are no spare sprites.</summary>
    /// <remarks>
    /// PUZZLE.BMX ships exactly two images and CIPHER.C blits one per bolt. Pinning the two counts
    /// together is what stops a third bolt being added with nothing to draw, or a latch being
    /// dropped and only heard.
    /// </remarks>
    [Fact]
    public void EveryBoltHasALatch() {
        Assert.Equal(CipherPuzzleSound.Bolts, CipherPuzzleLayout.LatchOriginsVga().Length);
    }

    /// <summary>The two latches are at different places, and neither mirrors the other.</summary>
    /// <remarks>
    /// (0x1e, 0x17) and (0x100, 0x14) — the y values differ, so a port that placed the second from
    /// the first by mirroring x would sit three pixels low. Both are on screen in VGA space.
    /// </remarks>
    [Fact]
    public void TheLatchesAreTwoDistinctOnScreenPositions() {
        (int X, int Y)[] latches = CipherPuzzleLayout.LatchOriginsVga();

        Assert.NotEqual(latches[0], latches[1]);
        Assert.NotEqual(latches[0].Y, latches[1].Y);
        Assert.All(latches, latch => {
            Assert.InRange(latch.X, 0, 319);
            Assert.InRange(latch.Y, 0, 199);
        });
    }

    /// <summary>The hold after the last bolt outlasts either gap between them.</summary>
    /// <remarks>
    /// 200 ticks against 150 and 60. It is the pause the closing line waits out, so collapsing it
    /// to the shorter of the two would put narration over the second latch landing.
    /// </remarks>
    [Fact]
    public void TheClosingHoldIsTheLongestWait() {
        Assert.All(CipherPuzzleSound.BoltDelaysSeconds,
            d => Assert.True(CipherPuzzleSound.AfterBoltsSeconds > d));
    }
}
