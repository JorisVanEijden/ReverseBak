namespace BetrayalAtKrondor.Tests.Animation;

using GameData.Resources.Animation.FrameCommands;

using Xunit;

/// <summary>
/// The TTM free-angle rotation's encoding, decoded at extraction rather than in the renderer.
/// </summary>
public class DrawImageRotatedTests {
    [Fact]
    public void TheQuarterTurnValueDecodesToExactlyFortyFive() {
        // 0xE000 is the one shipped value that lands on a round number, which makes it the anchor:
        // raw >> 4 = 0xE00 = 3584 units, 4096 - 3584 = 512, and 512/4096 of a turn is 45 degrees.
        Assert.Equal(45f, DrawImageRotated.DegreesFromRaw(0xE000), 3);
    }

    [Theory]
    // Every distinct rotation in the shipped tree.
    [InlineData(53280)] [InlineData(54544)] [InlineData(48768)] [InlineData(49408)]
    [InlineData(53984)] [InlineData(50160)] [InlineData(49568)] [InlineData(5632)]
    [InlineData(49488)] [InlineData(60544)] [InlineData(63664)] [InlineData(50800)]
    [InlineData(57344)] [InlineData(54384)] [InlineData(49952)] [InlineData(50288)]
    [InlineData(51472)] [InlineData(51152)] [InlineData(64688)]
    public void EveryShippedAngleRoundTripsExactly(int raw) {
        // The assembler writes TTMs back out, so a lossy decode would corrupt a re-saved animation.
        // The low nibble carries nothing and is zero in all shipped data, which is what makes this
        // safe — if a future file used it, this is the test that would fail.
        Assert.Equal(raw, DrawImageRotated.RawFromDegrees(DrawImageRotated.DegreesFromRaw(raw)));
    }

    [Fact]
    public void TheAngleRunsTheOppositeWayFromTheFileValue() {
        // Not a scale factor: a LARGER raw is a SMALLER angle. Decoding it as a plain
        // raw * 360/4096 would rotate every image the wrong way and still look like an angle.
        Assert.True(DrawImageRotated.DegreesFromRaw(60544) < DrawImageRotated.DegreesFromRaw(48768));
    }
}
