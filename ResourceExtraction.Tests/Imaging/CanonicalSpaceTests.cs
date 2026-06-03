namespace ResourceExtraction.Tests.Imaging;

using GameData.Resources.Menu;
using ResourceExtraction.Imaging;
using Xunit;

public class CanonicalSpaceTests {
    [Fact]
    public void Apply_UserInterface_ScalesMenuAndEntriesVga() {
        var ui = new UserInterface("REQ_TEST.DAT") {
            XPosition = 13, YPosition = 11, Width = 320, Height = 200,
            XOffset = 10, YOffset = 20,
            MenuEntries = [
                new UiElement { XPosition = 2, YPosition = 3, Width = 4, Height = 5 }
            ]
        };

        CanonicalSpace.Apply(ui);

        Assert.Equal(65, ui.XPosition);
        Assert.Equal(66, ui.YPosition);
        Assert.Equal(1600, ui.Width);
        Assert.Equal(1200, ui.Height);
        Assert.Equal(50, ui.XOffset);
        Assert.Equal(120, ui.YOffset);
        Assert.Equal(10, ui.MenuEntries[0].XPosition);
        Assert.Equal(18, ui.MenuEntries[0].YPosition);
        Assert.Equal(20, ui.MenuEntries[0].Width);
        Assert.Equal(30, ui.MenuEntries[0].Height);
    }
}
