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
}
