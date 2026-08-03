namespace ResourceExtraction.Tests.Layout;

using GameData.Resources.Layout;
using GameData.Resources.Menu;
using GameData.Resources.Label;
using Xunit;

public class LayoutHintTests {
    [Fact]
    public void LayoutHint_DefaultsToClassic() {
        var hint = new LayoutHint();
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
}
