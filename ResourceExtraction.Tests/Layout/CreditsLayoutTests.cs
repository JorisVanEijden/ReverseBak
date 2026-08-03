namespace ResourceExtraction.Tests.Layout;

using GameData.Resources.Credits;
using GameData.Resources.Layout;
using Xunit;

/// <summary>Faithfulness gate for the credits layout: these are exactly the constants
/// CreditsView.cs carried before the conversion (see the plan's table). If one of these
/// changes, the credits screen has moved relative to the original.</summary>
public class CreditsLayoutTests {
    [Fact]
    public void Defaults_MatchTheOriginalCanonicalGeometry() {
        var layout = new CreditsLayout();
        Assert.Equal(LayoutLength.Px(246f), layout.TitleY);
        Assert.Equal(LayoutLength.Px(324f), layout.WindowTop);
        Assert.Equal(LayoutLength.Px(948f), layout.WindowBottom);
        Assert.Equal(LayoutLength.Px(66f), layout.LineHeight);
        Assert.Equal(LayoutLength.Px(210f), layout.RoleLeftX);
        Assert.Equal(LayoutLength.Px(1385f), layout.NameRightX);
        Assert.Equal(LayoutLength.Px(800f), layout.CenterX);
        Assert.Equal(LayoutLength.Px(96f), layout.FadeTopBand);
        Assert.Equal(LayoutLength.Px(102f), layout.FadeBottomBand);
        Assert.Equal(LayoutLength.Px(48f), layout.FontSize);
        Assert.Equal(LayoutLength.Px(20f), layout.LeaderDotPitch);
        Assert.Equal(LayoutLength.Px(2.5f), layout.LeaderDotRadius);
        Assert.Equal(LayoutLength.Px(10f), layout.LeaderGap);
    }

    [Fact]
    public void CreditsData_HasALayoutByDefault() {
        Assert.NotNull(new CreditsData("CRED.DAT").Layout);
    }
}
