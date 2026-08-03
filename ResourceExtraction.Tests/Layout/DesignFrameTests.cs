namespace ResourceExtraction.Tests.Layout;

using GameData.Resources.Layout;
using ResourceExtraction.Imaging;
using Xunit;

public class DesignFrameTests {
    [Fact]
    public void CanonicalDimensions_AreDerivedFromTheVgaMode_NotHardcoded() {
        // 320x200 mode 13h, x5 horizontal / x6 vertical — the frame follows from the
        // mode and the aspect factors, so changing a factor changes the frame.
        Assert.Equal(320, AspectCorrection.VgaWidth);
        Assert.Equal(200, AspectCorrection.VgaHeight);
        Assert.Equal(320 * AspectCorrection.VgaScaleX, AspectCorrection.CanonicalWidth);
        Assert.Equal(200 * AspectCorrection.VgaScaleY, AspectCorrection.CanonicalHeight);
        Assert.Equal(1600, AspectCorrection.CanonicalWidth);
        Assert.Equal(1200, AspectCorrection.CanonicalHeight);
    }

    [Fact]
    public void DesignFrame_DefaultsToContain() {
        Assert.Equal(LayoutFit.Contain, new DesignFrame().Fit);
    }

    [Fact]
    public void DesignFrame_RoundTripsThroughJson() {
        var original = new DesignFrame { Width = 1600, Height = 1200, Fit = LayoutFit.Fill };
        DesignFrame restored = System.Text.Json.JsonSerializer.Deserialize<DesignFrame>(
            System.Text.Json.JsonSerializer.Serialize(original))!;
        Assert.Equal(1600, restored.Width);
        Assert.Equal(1200, restored.Height);
        Assert.Equal(LayoutFit.Fill, restored.Fit);
    }
}
