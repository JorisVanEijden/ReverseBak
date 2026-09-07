namespace BetrayalAtKrondor.Tests.Shop;

using System.Collections.Generic;
using System.Linq;
using GameData;
using GameData.Resources.Data;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using GameData.Resources.Shop;
using Xunit;

/// <summary>
/// The order a shop puts its shelf in — <c>cmbinv_item_compare</c>'s <c>sort_mode == 1</c> branch.
/// </summary>
/// <remarks>
/// <b>Measured against the original, not inferred.</b> Fletcher's Post in LaMut (GDS1C), chapter 1,
/// on the shipped <c>probe.G01/SAVE00</c>. The fixture below is that container's stored array
/// verbatim; the expectation is the order the original actually displayed across its three pages on
/// 2026-09-07.
///
/// <para>The port had been sorting a shelf with the PACK rule (footprint descending), which opens
/// Fletcher's Post on the Standard Kingdom Armor — a four-slot item — where the original opens on
/// the Broadsword.</para>
/// </remarks>
public class ShopShelfOrderTests {
    private const int VisitingChapter = 1;

    [Fact]
    public void TheShelfComesOutInTheOrDERTheORIGINALDisplays() {
        RuntimeContainer shelf = FletchersPost();

        InventoryOrder.Consolidate(shelf, Catalog(), equippedOrder: false,
            shopChapter: VisitingChapter);

        // Page 1 was Broadsword / Light Crossbow / Tsurani Light Crossbow / Elven Crossbow /
        // Quarrels / Elven Quarrels; page 3 ended Weedwalkers / Kalem's / Magical Scroll /
        // Broadsword (69%), with the Medium Crossbow's slot blank.
        Assert.Equal(
            new[] { 18, 30, 33, 35, 36, 37, 38, 48, 76, 77, 84, 86, 90, 129, 133, 18, 31 },
            shelf.Items.Select(i => (int)i.ObjectId));
    }

    [Fact]
    public void TheSecondHandCopySinksBelowEveryPristineItem_NotJustItsOwnKind() {
        // The 69% Broadsword carries flags 0x0022 — the shop bought it off the party. It sorts
        // after Kalem's Dialectic (id 129), which a plain id sort would never do.
        RuntimeContainer shelf = FletchersPost();

        InventoryOrder.Consolidate(shelf, Catalog(), equippedOrder: false,
            shopChapter: VisitingChapter);

        int usedIndex = shelf.Items.ToList().FindIndex(i => (i.ItemFlags & ShopStock.ForSaleFlag) != 0);
        int lastPristine = shelf.Items.ToList().FindLastIndex(
            i => (i.ItemFlags & ShopStock.ForSaleFlag) == 0 && i.ObjectId != MediumCrossbow);

        Assert.True(usedIndex > lastPristine,
            $"the second-hand copy is at {usedIndex}, the last pristine item at {lastPristine}");
    }

    [Fact]
    public void StockTheChapterHasNotIntroducedSinksBelowEVERYTHING() {
        // Availability is the FIRST key, so a future item goes last — below even the second-hand
        // one. That is what puts the Medium Crossbow's blank slot on the last page.
        RuntimeContainer shelf = FletchersPost();

        InventoryOrder.Consolidate(shelf, Catalog(), equippedOrder: false,
            shopChapter: VisitingChapter);

        Assert.Equal(MediumCrossbow, shelf.Items[^1].ObjectId);
    }

    [Fact]
    public void InAChapterThatHasItTheFutureStockTakesItsIdPosition() {
        // The same shelf visited in chapter 3: the Medium Crossbow (31) is available and sorts by
        // id, between the Light Crossbow (30) and the Tsurani (33).
        RuntimeContainer shelf = FletchersPost();

        InventoryOrder.Consolidate(shelf, Catalog(), equippedOrder: false, shopChapter: 3);

        List<int> ids = shelf.Items.Select(i => (int)i.ObjectId).ToList();
        Assert.Equal(2, ids.IndexOf(MediumCrossbow) - ids.IndexOf(30) + 1);
        Assert.True(ids.IndexOf(MediumCrossbow) < ids.IndexOf(33));
    }

    [Fact]
    public void APackIsStillSortedTheOtherWay() {
        // The pack rule is unchanged and still footprint-first: passing no chapter must not
        // silently switch every inventory in the game to the shop's keys.
        RuntimeContainer pack = FletchersPost();

        InventoryOrder.Consolidate(pack, Catalog(), equippedOrder: false);

        Assert.Equal(Armor, pack.Items[0].ObjectId);   // four slots, so first under the pack rule
    }

    private const byte MediumCrossbow = 31;
    private const byte Armor = 48;

    /// <summary>GDS1C's container as probe.G01/SAVE00 stores it, in file order.</summary>
    private static RuntimeContainer FletchersPost() {
        (byte id, byte cond, ushort flags)[] slots = {
            (18, 100, 0), (30, 100, 0), (33, 100, 0), (35, 100, 0), (36, 25, 0), (37, 25, 0),
            (38, 25, 0), (48, 100, 0), (76, 0, 0), (77, 0, 0), (84, 3, 0), (86, 6, 0),
            (90, 0, 0), (129, 100, 0), (133, 11, 0), (31, 100, 0), (18, 69, 0x0022),
        };
        var c = new RuntimeContainer { Capacity = 24, ContainerType = SaveGameContainerType.FixedWorldItem, IsShop = true };
        foreach ((byte id, byte cond, ushort flags) in slots) {
            c.Items.Add(new RuntimeItem(id, cond, flags));
        }

        return c;
    }

    /// <summary>The chapter and footprint of each id above; everything is chapter 1 but the 31.</summary>
    private static ObjectInfoSet Catalog() {
        var slotsFor = new Dictionary<int, int> { [48] = 4, [18] = 2, [30] = 2, [31] = 2, [33] = 2, [35] = 2 };
        var infos = new List<ObjectInfo>();
        foreach (int id in new[] { 18, 30, 31, 33, 35, 36, 37, 38, 48, 76, 77, 84, 86, 90, 129, 133 }) {
            infos.Add(new ObjectInfo("O") {
                Number = id,
                Name = $"item{id}",
                ChapterNumber = id == MediumCrossbow ? 3 : 1,
                InventorySlots = slotsFor.TryGetValue(id, out int n) ? n : 1,
            });
        }

        return new ObjectInfoSet("O", infos);
    }
}
