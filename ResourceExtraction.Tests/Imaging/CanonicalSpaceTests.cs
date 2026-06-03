namespace ResourceExtraction.Tests.Imaging;

using GameData.Resources.Label;
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

    [Fact]
    public void Apply_LabelSet_ScalesLabelPositionsVga() {
        var set = new LabelSet("LBL_TEST.DAT") {
            Labels = [
                new Label { XPosition = 13, YPosition = 11 }
            ]
        };

        CanonicalSpace.Apply(set);

        Assert.Equal(65, set.Labels[0].XPosition);
        Assert.Equal(66, set.Labels[0].YPosition);
    }

    [Fact]
    public void Apply_BookResource_ScalesPageImageParagraphReservedEga() {
        var book = new GameData.Resources.Book.BookResource("C11.BOK") {
            Pages = [
                new GameData.Resources.Book.Page {
                    XOffset = 40, YOffset = 80, Width = 554, Height = 240,
                    Images = [ new GameData.Resources.Book.BookImage { X = 530, Y = 10 } ],
                    Paragraphs = [ new GameData.Resources.Book.Paragraph {
                        XOffset = 40, YOffset = 80, Width = 554,
                        StartIndent = 10, LineSpacing = 12, InterParagraphSpacing = 6
                    } ],
                    ReservedAreas = [ new GameData.Resources.Book.ReservedArea {
                        X = 40, Y = 80, X2 = 594, Y2 = 320
                    } ]
                }
            ]
        };

        CanonicalSpace.Apply(book);

        var page = book.Pages[0];
        Assert.Equal(80, page.XOffset);    // 40 * 2
        Assert.Equal(219, page.YOffset);   // round(80 * 96 / 35)
        Assert.Equal(1108, page.Width);    // 554 * 2
        Assert.Equal(658, page.Height);    // round(240 * 96 / 35) = round(658.29)
        Assert.Equal(1060, page.Images[0].X);  // 530 * 2
        Assert.Equal(27, page.Images[0].Y);    // round(10 * 96 / 35) = round(27.43)
        Assert.Equal(80, page.Paragraphs[0].XOffset);
        Assert.Equal(219, page.Paragraphs[0].YOffset);
        Assert.Equal(1108, page.Paragraphs[0].Width);
        Assert.Equal(20, page.Paragraphs[0].StartIndent);          // 10 * 2
        Assert.Equal(33, page.Paragraphs[0].LineSpacing);          // round(12 * 96 / 35) = round(32.9)
        Assert.Equal(16, page.Paragraphs[0].InterParagraphSpacing);// round(6 * 96 / 35) = round(16.46)
        Assert.Equal(80, page.ReservedAreas[0].X);
        Assert.Equal(219, page.ReservedAreas[0].Y);
        Assert.Equal(1188, page.ReservedAreas[0].X2);  // 594 * 2
        Assert.Equal(878, page.ReservedAreas[0].Y2);   // round(320 * 96 / 35) = round(877.71)
    }
}
