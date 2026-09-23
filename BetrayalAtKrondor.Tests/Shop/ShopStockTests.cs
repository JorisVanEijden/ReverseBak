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
    public void ABuyerWithNoRoomBuysNothingAndKeepsTheGold() {
        // The refusal InventoryMenu.CompletePurchaseAsync has to SAY something about: at Romney's
        // tavern on 2026-09-10 the offer was accepted, Buy returned false on the room check and
        // nothing moved or was said, which reads as a dead Accept button (TASK-413).
        RuntimeContainer shop = Shop(4, Owned(OtherId));
        var buyer = new RuntimeContainer { Capacity = 1, ContainerType = SaveGameContainerType.Inventory };
        buyer.Items.Add(new RuntimeItem((byte)CheapId, 1, 0));
        var gold = 100;

        Assert.False(ShopStock.Buy(shop, buyer, shop.Items[0], Objects(), 40, ref gold));

        Assert.Equal(100, gold);
        Assert.Single(buyer.Items);
        Assert.Single(shop.Items);
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

    // TASK-625: Buy tested the slot budget alone, so a buyer at the budget was refused a stackable
    // they ALREADY carry -- a merge that costs no new slot, and which the delivery below performs
    // anyway by adding then consolidating. Measured at Romney: a member at 20/20 slots holding 2
    // Rations ran the whole offer and kept its gold.
    private static (ObjectInfoSet objs, RuntimeContainer buyer, RuntimeContainer shop) SlotFullBuyer(
        bool alreadyHoldsTheStack) {
        const int RationsId = 72;
        var list = new List<ObjectInfo> {
            new ObjectInfo("r") {
                Number = RationsId, Name = "Rations", Price = 1, InventorySlots = 1,
                MaxAmount = 14, Flags = (ObjectFlags)0x800,
            },
        };
        var held = new List<RuntimeItem>();
        int filler = alreadyHoldsTheStack ? 19 : 20;
        for (var i = 0; i < filler; i++) {
            var id = (byte)(100 + i);
            list.Add(new ObjectInfo("x" + i) { Number = id, Price = 1, InventorySlots = 1, MaxAmount = 1 });
            held.Add(new RuntimeItem(id, 1, 0));
        }
        if (alreadyHoldsTheStack) { held.Add(new RuntimeItem(RationsId, 2, 0)); }

        var buyer = new RuntimeContainer { Capacity = 24, ContainerType = SaveGameContainerType.Inventory };
        buyer.Items.AddRange(held);
        var shop = new RuntimeContainer { Capacity = 28, ContainerType = SaveGameContainerType.FixedWorldItem };
        shop.Items.Add(new RuntimeItem(RationsId, 3, 0));

        return (new ObjectInfoSet("O", list), buyer, shop);
    }

    [Fact] public void Buy_DeliversAStackableTheBuyerAlreadyCarries_WhenSlotFull() {
        var (objs, buyer, shop) = SlotFullBuyer(alreadyHoldsTheStack: true);
        var gold = 100;
        var ok = ShopStock.Buy(shop, buyer, shop.Items[0], objs, 10, ref gold);

        Assert.True(ok);
        Assert.Equal(90, gold);
        var stack = buyer.Items.Find(i => i.ObjectId == 72);
        Assert.NotNull(stack);
        Assert.Equal(5, stack.Variable);   // 2 held + 3 bought, merged into the one stack
    }

    // The control: without it the fix passes by never refusing, and the slot budget stops meaning
    // anything for a buyer who is genuinely out of room.
    [Fact] public void Buy_StillRefusesAStackableTheBuyerDoesNotCarry_WhenSlotFull() {
        var (objs, buyer, shop) = SlotFullBuyer(alreadyHoldsTheStack: false);
        var gold = 100;
        var ok = ShopStock.Buy(shop, buyer, shop.Items[0], objs, 10, ref gold);

        Assert.False(ok);
        Assert.Equal(100, gold);           // nothing charged
        Assert.DoesNotContain(buyer.Items, i => i.ObjectId == 72);
    }
}
