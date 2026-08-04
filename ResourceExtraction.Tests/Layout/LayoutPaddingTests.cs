namespace ResourceExtraction.Tests.Layout;

using System.Text.Json;
using GameData.Resources.Layout;
using Xunit;

public class LayoutPaddingTests {
    [Fact]
    public void LayoutPadding_DefaultsToAutoOnAllFourSides() {
        var padding = new LayoutPadding();
        Assert.Equal(LayoutLength.Auto, padding.Left);
        Assert.Equal(LayoutLength.Auto, padding.Top);
        Assert.Equal(LayoutLength.Auto, padding.Right);
        Assert.Equal(LayoutLength.Auto, padding.Bottom);
    }

    [Fact]
    public void LayoutPadding_RoundTripsThroughJson() {
        // 143/87 — asymmetric, non-round: nothing an implementation would hardcode.
        var original = new LayoutPadding {
            Left = LayoutLength.Px(143f),
            Bottom = LayoutLength.Percent(87f)
        };

        LayoutPadding restored = JsonSerializer.Deserialize<LayoutPadding>(JsonSerializer.Serialize(original))!;

        Assert.Equal(LayoutLength.Px(143f), restored.Left);
        Assert.Equal(LayoutLength.Percent(87f), restored.Bottom);
        // Untouched sides must keep their faithful Auto default — a partial value should not
        // zero out the rest.
        Assert.Equal(LayoutLength.Auto, restored.Top);
        Assert.Equal(LayoutLength.Auto, restored.Right);
    }

    [Fact]
    public void LayoutHint_Padding_DefaultsToNull() {
        Assert.Null(new LayoutHint().Padding);
    }

    [Fact]
    public void LayoutHint_Padding_RoundTripsThroughJson_IndependentlyOfFlowGridAndPosition() {
        var original = new LayoutHint {
            Position = LayoutPosition.Absolute,
            Flow = null,
            Grid = null,
            Padding = new LayoutPadding {
                Left = LayoutLength.Px(143f),
                Bottom = LayoutLength.Percent(87f)
            }
        };

        LayoutHint restored = JsonSerializer.Deserialize<LayoutHint>(JsonSerializer.Serialize(original))!;

        Assert.NotNull(restored.Padding);
        Assert.Equal(LayoutLength.Px(143f), restored.Padding!.Left);
        Assert.Equal(LayoutLength.Percent(87f), restored.Padding.Bottom);
        Assert.Equal(LayoutLength.Auto, restored.Padding.Top);
        Assert.Equal(LayoutLength.Auto, restored.Padding.Right);
        // Padding coexists with Position/Flow/Grid rather than displacing them.
        Assert.Equal(LayoutPosition.Absolute, restored.Position);
        Assert.Null(restored.Flow);
        Assert.Null(restored.Grid);
    }
}
