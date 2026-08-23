namespace BetrayalAtKrondor.Tests.Menu;

using GameData.Resources.Menu;

using Xunit;

/// <summary>
/// Which of the two click cues an element plays — <c>menu_resolveHoverAndClick</c> @0x2c97f.
/// </summary>
public class UiElementSoundTests {
    private static UiElement Element(int soundFlags, int clickSound = 0) =>
        new UiElement { SoundFlags = soundFlags, ClickSound = clickSound };

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
