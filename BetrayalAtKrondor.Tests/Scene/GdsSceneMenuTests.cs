namespace BetrayalAtKrondor.Tests.Scene;

using GameData.Resources.Menu;
using System.Linq;
using GameData.Resources.Scene;
using Xunit;

public class GdsSceneMenuTests {
    private static UserInterface Frame(int x = 100, int y = 60) =>
        new("REQ_GDS.DAT") { XPosition = x, YPosition = y };

    private static GdsHotspot Spot(int x = 500, int y = 400, int cursor = 3) =>
        new() { XPosition = x, YPosition = y, Width = 80, Height = 40, Cursor = cursor };

    private static GdsScene Scene(params GdsHotspot[] hotspots) =>
        new("GDS11A") { Hotspots = hotspots };

    [Fact]
    public void HotspotRectIsRebasedOntoTheLayoutOrigin() {
        UiElement[] built = GdsSceneMenu.BuildElements(Scene(Spot()), Frame(), 1, false, null);

        UiElement only = Assert.Single(built);
        Assert.Equal(400, only.XPosition); // 500 - 100
        Assert.Equal(340, only.YPosition); // 400 - 60
        // The size is not an offset and is copied through unchanged.
        Assert.Equal(80, only.Width);
        Assert.Equal(40, only.Height);
    }

    [Fact]
    public void ActionIdsCountFromTheHotspotArrayIndex() {
        UiElement[] built = GdsSceneMenu.BuildElements(Scene(Spot(), Spot(), Spot()), Frame(), 1, false, null);

        Assert.Equal([128, 129, 130], built.Select(e => e.ActionId));
    }

    [Fact]
    public void AHiddenHotspotStillConsumesItsActionId() {
        var hidden = Spot();
        hidden.HiddenInChapters = [2];

        UiElement[] built = GdsSceneMenu.BuildElements(Scene(Spot(), hidden, Spot()), Frame(), 2, false, null);

        // Index 1 is dropped, so the survivors keep 128 and 130 — they do not close up to 128/129.
        // Renumbering them would point every later hotspot at the wrong record.
        Assert.Equal([128, 130], built.Select(e => e.ActionId));
    }

    [Fact]
    public void CursorIsConvertedOutOfTheFilesOneBasedNumbering() {
        UiElement[] built = GdsSceneMenu.BuildElements(Scene(Spot(cursor: 3)), Frame(), 1, false, null);

        Assert.Equal(2, Assert.Single(built).Cursor);
    }

    [Fact]
    public void NoCursorBecomesTheDefaultArrowRatherThanCursorZero() {
        Assert.Equal(-1, GdsSceneMenu.CursorIndexFor(Spot(cursor: 0)));
    }

    [Fact]
    public void HotspotsAreClickAreasWithNoArtwork() {
        UiElement only = Assert.Single(GdsSceneMenu.BuildElements(Scene(Spot()), Frame(), 1, false, null));

        Assert.Equal(ElementType.ClickArea, only.ElementType);
        Assert.False(only.Visible);
    }

    [Fact]
    public void PreserveKeepsAHotspotTheChapterWouldHaveHidden() {
        var hidden = Spot();
        hidden.HiddenInChapters = [2];

        Assert.Empty(GdsSceneMenu.BuildElements(Scene(hidden), Frame(), 2, false, null));
        Assert.Single(GdsSceneMenu.BuildElements(Scene(hidden), Frame(), 2, true, null));
    }

    [Fact]
    public void ASceneWithoutItsLayoutBuildsNothing() {
        // The rebase cannot be skipped, so there is no meaningful result without the layout.
        Assert.Empty(GdsSceneMenu.BuildElements(Scene(Spot()), null, 1, false, null));
    }
}
