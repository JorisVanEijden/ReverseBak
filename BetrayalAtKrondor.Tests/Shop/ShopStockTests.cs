namespace BetrayalAtKrondor.Tests.Shop;

using GameData;
using GameData.Resources.Data;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using GameData.Resources.Shop;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The transfer half of a shop transaction — which slot a sale lands in, and what actually moves.
/// The displacement rule is the one worth pinning: a full shop drops its DEAREST stock.
/// </summary>
public class ShopStockTests {
    private const int CheapId = 10;
    private const int DearId = 11;
    private const int OtherId = 12;

    private static ObjectInfo Info(int number, int price) =>
        new ObjectInfo("test") {
            Number = number,
            Price = price,
            ShopType = (int)ShopItemCategories.Miscellaneous,
            MaxAmount = 1,
        };

    private static ObjectInfoSet Objects() {
        var items = new List<ObjectInfo>();
        for (var id = 0; id <= OtherId; id++) {
            items.Add(Info(id, id == CheapId ? 5 : id == DearId ? 500 : id == OtherId ? 50 : 1));
        }
        return new ObjectInfoSet("OBJINFO.DAT", items);
    }

    private static RuntimeContainer Shop(int capacity, params RuntimeItem[] stock) {
        var shop = new RuntimeContainer { Capacity = capacity, ContainerType = SaveGameContainerType.FixedWorldItem };
        foreach (RuntimeItem item in stock) {
            shop.Items.Add(item);
        }
        return shop;
    }

    private static RuntimeItem Stocked(int objectId) =>
        new RuntimeItem((byte)objectId, 1, ShopStock.ForSaleFlag);

    private static RuntimeItem Owned(int objectId) => new RuntimeItem((byte)objectId, 1, 0);

    [Fact]
    public void AShopWithRoomExtendsItsStock() {
        RuntimeContainer shop = Shop(4, Stocked(CheapId));

        int slot = ShopStock.SelectSellSlot(shop, Objects(), out bool isNew);

        Assert.Equal(1, slot);
        Assert.True(isNew);
    }

    [Fact]
    public void AFullShopDisplacesItsMostValuableStock() {
        // Economically backwards, and exactly what the code does: the scan keeps a candidate only
        // when it beats the incumbent, so it lands on the highest base price.
        RuntimeContainer shop = Shop(3, Stocked(CheapId), Stocked(DearId), Stocked(OtherId));

        int slot = ShopStock.SelectSellSlot(shop, Objects(), out bool isNew);

        Assert.Equal(1, slot);      // the 500-price item
        Assert.False(isNew);
    }

    [Fact]
    public void OnlyTheShopsOwnStockCanBeDisplaced() {
        // A slot without the for-sale flag is infinite stock, never bought in — so it is never the
        // one dropped, even when it is the dearest thing on the shelf.
        RuntimeContainer shop = Shop(2, Owned(DearId), Stocked(CheapId));

        int slot = ShopStock.SelectSellSlot(shop, Objects(), out _);

        Assert.Equal(1, slot);
    }

    [Fact]
    public void AFullShopOfNothingItBoughtHasNoRoom() {
        RuntimeContainer shop = Shop(2, Owned(CheapId), Owned(DearId));
        var seller = new RuntimeContainer { Capacity = 10 };
        RuntimeItem selling = Owned(OtherId);
        seller.Items.Add(selling);
        var gold = 0;

        ShopStock.SellResult result = ShopStock.Sell(shop, seller, selling, Objects(),
            ShopItemCategories.Miscellaneous, 40, ref gold);

        Assert.Equal(ShopStock.SellResult.NoRoom, result);
        Assert.Equal(0, gold);
        Assert.Contains(selling, seller.Items);
    }

    [Fact]
    public void ASoldItemLeavesTheSellerAndIsPaidFor() {
        RuntimeContainer shop = Shop(4);
        var seller = new RuntimeContainer { Capacity = 10 };
        RuntimeItem selling = Owned(OtherId);
        seller.Items.Add(selling);
        var gold = 100;

        ShopStock.SellResult result = ShopStock.Sell(shop, seller, selling, Objects(),
            ShopItemCategories.Miscellaneous, 40, ref gold);

        Assert.Equal(ShopStock.SellResult.Sold, result);
        Assert.Equal(140, gold);
        Assert.Empty(seller.Items);
        Assert.Single(shop.Items);
        Assert.NotEqual(0, shop.Items[0].ItemFlags & ShopStock.ForSaleFlag);
    }

    [Fact]
    public void TheDisplacedItemIsDestroyedNotRelocated() {
        RuntimeContainer shop = Shop(1, Stocked(DearId));
        var seller = new RuntimeContainer { Capacity = 10 };
        RuntimeItem selling = Owned(OtherId);
        seller.Items.Add(selling);
        var gold = 0;

        ShopStock.Sell(shop, seller, selling, Objects(), ShopItemCategories.Miscellaneous, 40, ref gold);

        Assert.Single(shop.Items);                       // count did not grow
        Assert.Equal(OtherId, shop.Items[0].ObjectId);   // and the dear one is simply gone
    }

    [Fact]
    public void AShopRefusesSomethingOutsideItsCategoriesItDoesNotStock() {
        RuntimeContainer shop = Shop(4);
        var seller = new RuntimeContainer { Capacity = 10 };
        RuntimeItem selling = Owned(OtherId);
        seller.Items.Add(selling);
        var gold = 0;

        ShopStock.SellResult result = ShopStock.Sell(shop, seller, selling, Objects(),
            ShopItemCategories.Keys, 40, ref gold);   // shop trades in something else

        Assert.Equal(ShopStock.SellResult.NotInterested, result);
        Assert.Equal(0, gold);
    }

    [Fact]
    public void ButItTakesAnotherOfSomethingItAlreadyStocks() {
        // The already-in-stock escape covers both the category test and the zero-price test.
        RuntimeContainer shop = Shop(4, Stocked(OtherId));
        var seller = new RuntimeContainer { Capacity = 10 };
        RuntimeItem selling = Owned(OtherId);
        seller.Items.Add(selling);
        var gold = 0;

        ShopStock.SellResult result = ShopStock.Sell(shop, seller, selling, Objects(),
            ShopItemCategories.Keys, 40, ref gold);

        Assert.Equal(ShopStock.SellResult.Sold, result);
    }

    [Fact]
    public void BuyingBoughtInStockConsumesTheShopsSlot() {
        RuntimeContainer shop = Shop(4, Stocked(OtherId));
        var buyer = new RuntimeContainer { Capacity = 10, ContainerType = SaveGameContainerType.Inventory };
        var gold = 100;

        Assert.True(ShopStock.Buy(shop, buyer, shop.Items[0], Objects(), 40, ref gold));

        Assert.Empty(shop.Items);
        Assert.Single(buyer.Items);
        Assert.Equal(60, gold);
        Assert.Equal(0, buyer.Items[0].ItemFlags & ShopStock.ForSaleFlag);
    }

    [Fact]
    public void BuyingInfiniteStockCopiesItAndLeavesTheShelfFull() {
        // A slot the shop never bought in is a bottomless supply — this is how a shop sells the
        // same rations forever.
        RuntimeContainer shop = Shop(4, Owned(OtherId));
        var buyer = new RuntimeContainer { Capacity = 10, ContainerType = SaveGameContainerType.Inventory };
        var gold = 100;

        Assert.True(ShopStock.Buy(shop, buyer, shop.Items[0], Objects(), 40, ref gold));

        Assert.Single(shop.Items);
        Assert.Single(buyer.Items);
        Assert.Equal(60, gold);
    }

    [Fact]
    public void APartyThatCannotAffordItBuysNothing() {
        RuntimeContainer shop = Shop(4, Stocked(OtherId));
        var buyer = new RuntimeContainer { Capacity = 10, ContainerType = SaveGameContainerType.Inventory };
        var gold = 10;

        Assert.False(ShopStock.Buy(shop, buyer, shop.Items[0], Objects(), 40, ref gold));

        Assert.Equal(10, gold);
        Assert.Single(shop.Items);
        Assert.Empty(buyer.Items);
    }
}
