namespace BetrayalAtKrondor.Tests.Shop;

using GameData.Resources.Shop;
using Xunit;

/// <summary>One button, three meanings, and only one direction that wraps.</summary>
public class ShopPagingTests {
    [Fact]
    public void PlainClick_AdvancesWhileThereIsStockToShow() {
        Assert.Equal(1, ShopPaging.Turned(0, itemCount: 15, ShopPaging.Turn.Next));
        Assert.Equal(2, ShopPaging.Turned(1, itemCount: 15, ShopPaging.Turn.Next));
    }

    [Fact]
    public void PlainClick_WrapsWhenTheNextPageWouldStartPastTheEnd() {
        // 15 items = pages 0,1,2 (the last holding three). Page 3 would start at item 18.
        Assert.Equal(0, ShopPaging.Turned(2, itemCount: 15, ShopPaging.Turn.Next));
    }

    [Fact]
    public void ExactlyOnePageOfStock_HasNowhereToGo() {
        Assert.Equal(0, ShopPaging.Turned(0, itemCount: 6, ShopPaging.Turn.Next));
        Assert.False(ShopPaging.Pages(6));
        Assert.True(ShopPaging.Pages(7));
    }

    [Fact]
    public void ShiftGoesBack_AndStopsAtTheFirstPageRatherThanWrapping() {
        Assert.Equal(1, ShopPaging.Turned(2, itemCount: 15, ShopPaging.Turn.Previous));
        Assert.Equal(0, ShopPaging.Turned(0, itemCount: 15, ShopPaging.Turn.Previous));
    }

    [Fact]
    public void RightShiftReturnsToTheFirstPage() =>
        Assert.Equal(0, ShopPaging.Turned(2, itemCount: 15, ShopPaging.Turn.First));

    /// <summary>Stock can shrink under a later page — the player just bought the last of it.</summary>
    [Fact]
    public void APageThatNoLongerExists_SnapsToTheFront() =>
        Assert.Equal(0, ShopPaging.Turned(0, itemCount: 3, ShopPaging.Turn.Next));

    [Fact]
    public void FirstItemIsThePageTimesSix() {
        Assert.Equal(0, ShopPaging.FirstItem(0));
        Assert.Equal(12, ShopPaging.FirstItem(2));
    }
}
