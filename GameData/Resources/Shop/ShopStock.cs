namespace GameData.Resources.Shop;

using GameData.Resources.Inventory;
using GameData.Resources.Object;

/// <summary>
/// Moving goods in and out of a shop's stock — the transfer half of <c>shop_sell_item</c> and
/// <c>BuyItem</c>, which <see cref="ShopPricing"/> deliberately leaves alone (it only answers what
/// something costs).
/// </summary>
public static class ShopStock {
    /// <summary>
    /// The flag marking a slot as the shop's own for-sale stock. Set when a shop takes an item in,
    /// cleared when one is sold to the player, and — the part that matters here — the only slots a
    /// full shop is willing to displace.
    /// </summary>
    public const ushort ForSaleFlag = (ushort)ItemFlags.Unknown2;

    /// <summary>The outcome of offering an item to a shop.</summary>
    public enum SellResult {
        /// <summary>The shop took it and paid.</summary>
        Sold,

        /// <summary>"I have no use for such an item" — outside its categories and not already stocked.</summary>
        NotInterested,

        /// <summary>Full, with nothing displaceable: every slot is something the shop did not buy.</summary>
        NoRoom,
    }

    /// <summary>
    /// Which slot a sold item would land in.
    /// </summary>
    /// <param name="isNewSlot">
    /// True when the shop had room and the item extends its stock; false when an existing slot is
    /// being displaced, in which case the item already there is <b>overwritten and lost</b> — the
    /// count does not grow.
    /// </param>
    /// <returns>The slot index, or -1 when the shop is full of things it cannot displace.</returns>
    /// <remarks>
    /// <b>A full shop drops its most valuable stock to take yours.</b> The scan keeps a candidate
    /// only when it beats the incumbent, so it ends on the <i>highest</i> base price, not the
    /// lowest. That is economically backwards and it is what the code does — worth pinning before
    /// someone "corrects" it into picking the cheapest.
    /// </remarks>
    public static int SelectSellSlot(RuntimeContainer shop, ObjectInfoSet objects,
        out bool isNewSlot) {
        isNewSlot = false;
        if (shop == null) {
            return -1;
        }

        if (shop.Items.Count < shop.Capacity) {
            isNewSlot = true;
            return shop.Items.Count;
        }

        int best = -1;
        int bestPrice = 0;
        for (var i = 0; i < shop.Items.Count; i++) {
            RuntimeItem candidate = shop.Items[i];
            if ((candidate.ItemFlags & ForSaleFlag) == 0) {
                continue; // not the shop's own stock; never displaced
            }
            int price = objects?.GetById(candidate.ObjectId)?.Price ?? 0;
            if (best >= 0 && bestPrice >= price) {
                continue;
            }
            best = i;
            bestPrice = price;
        }
        return best;
    }

    /// <summary>
    /// Completes a sale the player has already accepted: the item moves into the shop's stock and
    /// the party is paid.
    ///
    /// <para>Pricing is the caller's — pass what <see cref="ShopPricing.SellPrice"/> produced, since
    /// the original shows the offer and only transfers once the player agrees.</para>
    /// </summary>
    /// <param name="shopCategories">
    /// What this shop trades in, from its <c>SaveGameContainerShopData</c>. Passed rather than read
    /// off the container because <see cref="RuntimeContainer"/> is the generic one that also backs
    /// corpses and bags; in the original the shop block is a separate subrecord too.
    /// </param>
    /// <param name="partyGold">Adjusted by the price on a successful sale.</param>
    public static SellResult Sell(RuntimeContainer shop, RuntimeContainer seller,
        RuntimeItem item, ObjectInfoSet objects, ShopItemCategories shopCategories, long price,
        ref int partyGold) {
        if (shop == null || seller == null || item == null) {
            return SellResult.NotInterested;
        }

        ObjectInfo info = objects?.GetById(item.ObjectId);
        bool stocked = InventoryQuery.CountByKind(shop, item.ObjectId) != 0;
        // ObjectInfo.ShopType is the item's own category bitmask (canassa's wSub_flags), matched
        // against what the shop trades in — not the shop's type.
        if (!ShopPricing.WillBuy(info?.Price ?? 0, (ShopItemCategories)(info?.ShopType ?? 0),
                shopCategories, stocked)) {
            return SellResult.NotInterested;
        }

        int slot = SelectSellSlot(shop, objects, out bool isNewSlot);
        if (slot < 0) {
            return SellResult.NoRoom;
        }

        RuntimeItem taken = item.Clone();
        taken.ItemFlags |= ForSaleFlag;
        if (isNewSlot) {
            shop.Items.Add(taken);
        } else {
            shop.Items[slot] = taken; // the displaced item is gone, not relocated
        }
        shop.Dirty = true;

        seller.Items.Remove(item);
        seller.Dirty = true;
        partyGold += (int)price;
        return SellResult.Sold;
    }

    /// <summary>
    /// Hands a bought item to the player and takes the money.
    ///
    /// <para><b>Infinite stock is copied, not moved.</b> A shop slot without
    /// <see cref="ForSaleFlag"/> is a bottomless supply — the original leaves it in place and gives
    /// the buyer a copy, which is how a shop can sell the same rations forever. Only a slot the shop
    /// bought in is actually consumed.</para>
    /// </summary>
    /// <returns>False when the party cannot afford it or the item is not for sale.</returns>
    /// <param name="delivered">
    /// What the buyer receives, when that is not the item taken off the shelf — a day's rations is
    /// sold as one object and handed over as another (see <see cref="ShopPurchase.Delivered"/>).
    /// The substitution happens before the room check, as it does in the original, so it is the
    /// DELIVERED item that has to fit.
    /// </param>
    public static bool Buy(RuntimeContainer shop, RuntimeContainer buyer, RuntimeItem item,
        ObjectInfoSet objects, long price, ref int partyGold, RuntimeItem delivered = null) {
        if (shop == null || buyer == null || item == null || price < 0 || partyGold < price) {
            return false;
        }

        RuntimeItem handedOver = delivered ?? item;
        if (!InventoryTransfer.CanFit(buyer, handedOver, objects)) {
            return false;
        }

        bool shopOwned = (item.ItemFlags & ForSaleFlag) != 0;
        RuntimeItem given = handedOver.Clone();
        given.ItemFlags &= unchecked((ushort)~ForSaleFlag);

        if (shopOwned) {
            shop.Items.Remove(item);
            shop.Dirty = true;
        }

        buyer.Items.Add(given);
        InventoryOrder.Consolidate(buyer, objects,
            buyer.ContainerType == GameData.Resources.Data.SaveGameContainerType.Inventory);
        buyer.Dirty = true;
        partyGold -= (int)price;
        return true;
    }
}
