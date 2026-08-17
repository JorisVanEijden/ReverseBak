namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Config;
using GameData.Resources.Text;
using Xunit;

/// <summary>The inn's rest screen geometry — <c>UI_RestUntilTime</c> @0x4ff5c.</summary>
public class InnScreenLayoutTests {
    private const int VgaScaleX = 5;
    private const int VgaScaleY = 6;

    [Fact]
    public void TheDialPanelIsTheSameSubRectTheCampScreenUses() {
        // encamp_blitDialPanel @0x70a2b: VGA (13,11) 293x101, shared by both rest screens.
        Assert.Equal(13 * VgaScaleX, InnScreenLayout.PanelX);
        Assert.Equal(11 * VgaScaleY, InnScreenLayout.PanelY);
        Assert.Equal(293 * VgaScaleX, InnScreenLayout.PanelWidth);
        Assert.Equal(101 * VgaScaleY, InnScreenLayout.PanelHeight);
    }

    [Fact]
    public void TheFrameIsDrawnONTheArtworksEdgeNotAroundIt() {
        // The panel is blitted first and the bevel after, and the inner bottom line lands exactly
        // on the panel's last row (VGA 111 both). The outer rectangle still contains the panel, so
        // the artwork is never cropped -- but expecting the bevel to clear it is wrong, and that
        // expectation is what made these two rectangles look inconsistent.
        int panelLastRow = InnScreenLayout.PanelY + InnScreenLayout.PanelHeight - VgaScaleY;
        int panelLastColumn = InnScreenLayout.PanelX + InnScreenLayout.PanelWidth - VgaScaleX;

        Assert.Equal(InnScreenLayout.FrameInnerBottom, panelLastRow);
        Assert.True(InnScreenLayout.FrameOuterX < InnScreenLayout.PanelX);
        Assert.True(InnScreenLayout.FrameOuterY < InnScreenLayout.PanelY);
        Assert.True(InnScreenLayout.FrameOuterRight > panelLastColumn);
        Assert.True(InnScreenLayout.FrameOuterBottom > panelLastRow);
    }

    [Fact]
    public void TheHourlyRepaintSTOPSSHORTOfTheBevel() =>
        // Two rows shorter, so redrawing the dial every hour cannot erase the frame.
        Assert.True(InnScreenLayout.PanelY + InnScreenLayout.PanelRefreshHeight
            < InnScreenLayout.FrameInnerBottom);

    [Fact]
    public void TheInnerRectangleSitsInsideTheOuterOne() {
        Assert.True(InnScreenLayout.FrameInnerX > InnScreenLayout.FrameOuterX);
        Assert.True(InnScreenLayout.FrameInnerY > InnScreenLayout.FrameOuterY);
        Assert.True(InnScreenLayout.FrameInnerRight < InnScreenLayout.FrameOuterRight);
        Assert.True(InnScreenLayout.FrameInnerBottom < InnScreenLayout.FrameOuterBottom);
    }

    [Fact]
    public void TheBevelUsesFOURDistinctPens() =>
        // One colour for all four edges is a box outline, not a bevel.
        Assert.Equal(4, new System.Collections.Generic.HashSet<int> {
            InnScreenLayout.FrameInnerShadowPen, InnScreenLayout.FrameOuterShadowPen,
            InnScreenLayout.FrameInnerLightPen, InnScreenLayout.FrameOuterLightPen,
        }.Count);

    [Fact]
    public void TheAmountIsSixtyVgaPixelsPastTheCaption() =>
        // The original literally does `add si, 60` between the two draws (0x4ff34).
        Assert.Equal(60 * VgaScaleX, InnScreenLayout.PurseAmountX - InnScreenLayout.PurseLabelX);

    [Fact]
    public void ThePurseSitsInsideTheFrame() {
        Assert.InRange(InnScreenLayout.PurseY, InnScreenLayout.FrameOuterY, InnScreenLayout.FrameOuterBottom);
        Assert.InRange(InnScreenLayout.PurseLabelX, InnScreenLayout.FrameOuterX, InnScreenLayout.FrameOuterRight);
    }

    [Fact]
    public void TheCaptionIsInTheStringCatalog() =>
        Assert.Equal("Party Gold:", UiStrings.Get(InnScreenLayout.PurseLabelKey));
}
