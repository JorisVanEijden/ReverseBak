namespace BetrayalAtKrondor.Tests.Shop;

using GameData.Resources.Object;
using GameData.Resources.Shop;
using Xunit;

/// <summary>
/// Which of a shop's stock is on the shelf yet.
/// </summary>
/// <remarks>
/// Found by comparing Fletcher's Post, LaMut, against the original on 2026-09-07: the container
/// holds seventeen items and the original's chapter-1 shelf shows six. Our port drew a Medium
/// Crossbow, which is chapter-3 stock.
/// </remarks>
public class ShopStockChapterTests {
    [Theory]
    [InlineData(1, 1, true)]    // Broadsword and the crossbows: chapter 1 stock, chapter 1 visit
    [InlineData(3, 1, false)]   // Medium Crossbow on a chapter-1 visit — the case that was wrong
    [InlineData(3, 3, true)]    // ...and the same visit in chapter 3
    [InlineData(3, 9, true)]    // later chapters keep it
    [InlineData(0, 1, true)]    // an ungated item
    public void AnItemIsOnTheShelfOnceItsChapterHasArrived(int itemChapter, int visit, bool shown) {
        var info = new ObjectInfo("test") { ChapterNumber = itemChapter };

        Assert.Equal(shown, ShopStock.IsOfferedInChapter(info, visit));
    }

    [Fact]
    public void AnUnknownObjectIsNotHiddenByTheGate() {
        // A null record means the catalog could not answer, not that the item is future stock —
        // hiding it would silently empty a shelf if the catalog ever failed to load.
        Assert.True(ShopStock.IsOfferedInChapter(null, 1));
    }
}
