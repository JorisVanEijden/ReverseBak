namespace BetrayalAtKrondor.Tests.Menu;

using GameData.Resources.Menu;

using Xunit;

/// <summary>
/// The resolutions <c>UiElement</c> does for its consumers: which click cue plays
/// (<c>menu_resolveHoverAndClick</c> @0x2c97f) and which bitmap an icon index names
/// (<c>sub_seg029_A9</c> @0x2b579).
/// </summary>
public class UiElementTests {
    private static UiElement Element(int soundFlags, int clickSound = 0) =>
        new UiElement { SoundFlags = soundFlags, ClickSound = clickSound };

    [Theory]
    // Straight from the scheme: even -> BICONS1, odd -> BICONS2, sub-image = index / 2.
    [InlineData(10, "BICONS1.BMX#5")]
    [InlineData(11, "BICONS2.BMX#5")]
    // The bump: 51 becomes 52, so it lands in BICONS1 rather than BICONS2.
    [InlineData(50, "BICONS1.BMX#25")]
    [InlineData(51, "BICONS1.BMX#26")]
    [InlineData(52, "BICONS2.BMX#26")]
    public void TheIconIndexSplitsAcrossTwoFilesWithABumpAboveFifty(int combined, string expected) {
        Assert.Equal(expected, UiElement.IconKeyForCombined(combined));
    }

    [Fact]
    public void TheStateOffsetMustBeAddedBeforeResolving() {
        // The reason there is no "base key + N": across the bump the run is discontinuous, so a
        // consumer holding only the base key cannot reach the other states. Base 49's four states
        // skip BICONS2#25 entirely and repeat BICONS1.
        string[] states = { "BICONS2.BMX#24", "BICONS1.BMX#25", "BICONS1.BMX#26", "BICONS2.BMX#26" };
        for (var offset = 0; offset < states.Length; offset++) {
            Assert.Equal(states[offset], UiElement.IconKeyForCombined(49 + offset));
        }
    }

    [Fact]
    public void BitZeroGatesThePress_BitOneGatesTheRelease() {
        // The two bits are NOT interchangeable, and a swapped pair still produces a plausible
        // "sometimes silent" element — which is why this asserts each edge separately.
        Assert.Null(Element(1).PressSound);
        Assert.NotNull(Element(1).ReleaseSound);

        Assert.NotNull(Element(2).PressSound);
        Assert.Null(Element(2).ReleaseSound);
    }

    [Fact]
    public void ZeroMeansTheDefaultCue_NotSilence() {
        // ClickSound 0 is the commonest value in the shipped REQs. Reading it as "no sound" would
        // mute most of the UI, which is the mistake this resolution exists to prevent.
        Assert.Equal(UiElement.DefaultClickSoundId, Element(0).PressSound);
        Assert.Equal(UiElement.DefaultClickSoundId, Element(0).ReleaseSound);
    }

    [Fact]
    public void ACustomSoundOverridesBothCues() {
        Assert.Equal(18, Element(0, clickSound: 18).PressSound);
        Assert.Equal(18, Element(0, clickSound: 18).ReleaseSound);
    }

    [Fact]
    public void BothBitsSetIsGenuinelySilent() {
        // Inventory slots, the world-viewport hotspot, GDS zones, file-picker rows.
        Assert.Null(Element(3).PressSound);
        Assert.Null(Element(3).ReleaseSound);
        Assert.Null(Element(3, clickSound: 18).PressSound);
    }
}
