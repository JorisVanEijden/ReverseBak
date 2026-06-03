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
        Assert.False(hint.RelativeWidth);
        Assert.False(hint.RelativeHeight);
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
