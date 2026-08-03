namespace ResourceExtraction.Tests.Layout;

using System.Text.Json;
using GameData.Resources.Layout;
using GameData.Resources.Menu;
using GameData.Resources.Label;
using Xunit;

public class LayoutHintTests {
    [Fact]
    public void LayoutHint_DefaultsToClassic() {
        var hint = new LayoutHint();
        Assert.Equal(LayoutPosition.Absolute, hint.Position);
        Assert.Equal(LayoutAnchor.TopLeft, hint.Anchor);
        Assert.Equal(new NineSlice(0, 0, 0, 0), hint.Slice);
        Assert.Equal(LayoutLength.Auto, hint.Width);
        Assert.Equal(LayoutLength.Auto, hint.Height);
        Assert.Null(hint.AspectRatio);
        Assert.Null(hint.Flow);
    }

    [Fact]
    public void LayoutHint_CarriesExplicitLengths() {
        var hint = new LayoutHint { Width = LayoutLength.Px(200f), Height = LayoutLength.Percent(15f) };
        Assert.Equal(LayoutLength.Px(200f), hint.Width);
        Assert.Equal(LayoutLength.Percent(15f), hint.Height);
    }

    [Fact]
    public void LayoutFlow_DefaultsToWrappingRow() {
        var flow = new LayoutFlow();
        Assert.Equal(LayoutFlowDirection.Row, flow.Direction);
        Assert.True(flow.Wrap);
        Assert.Equal(LayoutFlowJustify.Start, flow.Justify);
        Assert.Equal(LayoutFlowAlign.Start, flow.Align);
        Assert.Equal(LayoutLength.Px(0f), flow.Gap);
    }

    [Fact]
    public void UiElement_HasClassicDefaultLayout() {
        Assert.Equal(LayoutAnchor.TopLeft, new UiElement().Layout.Anchor);
    }

    [Fact]
    public void Label_HasClassicDefaultLayout() {
        Assert.Equal(LayoutAnchor.TopLeft, new Label().Layout.Anchor);
    }

    [Fact]
    public void RoundTripsThroughJson_WithFlowAndAspectPopulated() {
        var original = new LayoutHint {
            Position = LayoutPosition.InFlow,
            Anchor = LayoutAnchor.BottomRight,
            Slice = new NineSlice(12, 10, 12, 14),
            Width = LayoutLength.Percent(50f),
            Height = LayoutLength.Px(180f),
            AspectRatio = new LayoutAspectRatio(10f, 9f),
            Flow = new LayoutFlow {
                Direction = LayoutFlowDirection.Column,
                Wrap = false,
                Justify = LayoutFlowJustify.SpaceBetween,
                Align = LayoutFlowAlign.Stretch,
                Gap = LayoutLength.Px(20f)
            }
        };

        string json = JsonSerializer.Serialize(original);
        LayoutHint restored = JsonSerializer.Deserialize<LayoutHint>(json)!;

        Assert.Equal(LayoutPosition.InFlow, restored.Position);
        Assert.Equal(LayoutAnchor.BottomRight, restored.Anchor);
        Assert.Equal(new NineSlice(12, 10, 12, 14), restored.Slice);
        Assert.Equal(LayoutLength.Percent(50f), restored.Width);
        Assert.Equal(LayoutLength.Px(180f), restored.Height);
        Assert.Equal(new LayoutAspectRatio(10f, 9f), restored.AspectRatio);
        Assert.NotNull(restored.Flow);
        Assert.Equal(LayoutFlowDirection.Column, restored.Flow!.Direction);
        Assert.False(restored.Flow.Wrap);
        Assert.Equal(LayoutFlowJustify.SpaceBetween, restored.Flow.Justify);
        Assert.Equal(LayoutFlowAlign.Stretch, restored.Flow.Align);
        Assert.Equal(LayoutLength.Px(20f), restored.Flow.Gap);
    }

    [Fact]
    public void LayoutHint_InsetsDefaultToAuto() {
        var hint = new LayoutHint();
        Assert.Equal(LayoutLength.Auto, hint.Left);
        Assert.Equal(LayoutLength.Auto, hint.Top);
        Assert.Equal(LayoutLength.Auto, hint.Right);
        Assert.Equal(LayoutLength.Auto, hint.Bottom);
    }

    [Fact]
    public void LayoutHint_InsetsRoundTripThroughJson() {
        var original = new LayoutHint {
            Left = LayoutLength.Px(210f),
            Right = LayoutLength.Px(215f),
            Top = LayoutLength.Percent(27f)
        };
        LayoutHint restored = JsonSerializer.Deserialize<LayoutHint>(JsonSerializer.Serialize(original))!;
        Assert.Equal(LayoutLength.Px(210f), restored.Left);
        Assert.Equal(LayoutLength.Px(215f), restored.Right);
        Assert.Equal(LayoutLength.Percent(27f), restored.Top);
        Assert.Equal(LayoutLength.Auto, restored.Bottom);
    }

    [Fact]
    public void RoundTripsThroughJson_WithNullsPreserved() {
        var original = new LayoutHint();
        LayoutHint restored = JsonSerializer.Deserialize<LayoutHint>(JsonSerializer.Serialize(original))!;
        Assert.Null(restored.AspectRatio);
        Assert.Null(restored.Flow);
        Assert.Equal(LayoutLength.Auto, restored.Width);
        Assert.Equal(LayoutLength.Auto, restored.Height);
    }
}
