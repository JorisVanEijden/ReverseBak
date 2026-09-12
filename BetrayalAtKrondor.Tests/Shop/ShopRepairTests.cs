namespace BetrayalAtKrondor.Tests.Shop;

using GameData;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using GameData.Resources.Shop;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The tinker (<c>modalscreen_inventory_request</c>, MODALSCR.C:503) — who he will work on, what he
/// charges, and how long the party loses to it.
/// </summary>
public class ShopRepairTests {
    private const int Broadsword = 18;
    private const int Chainmail = 40;
    private const int PlainCrossbow = 33;

    private static ObjectInfoSet Objects() => new ObjectInfoSet("O", new List<ObjectInfo> {
        new ObjectInfo("O") { Number = Broadsword, Name = "Broadsword", ObjectType = ObjectType.Sword, Price = 1000 },
        new ObjectInfo("O") { Number = Chainmail, Name = "Chainmail", ObjectType = ObjectType.Armor, Price = 2000 },
        new ObjectInfo("O") { Number = PlainCrossbow, Name = "Crossbow", ObjectType = ObjectType.Crossbow, Price = 5000 },
        new ObjectInfo("O") { Number = ShopRepair.BessyMaulerId, Name = "Bessy Mauler", ObjectType = ObjectType.Crossbow, Price = 10000 },
        new ObjectInfo("O") { Number = ShopRepair.HeavyBowstringId, Name = "Heavy Bowstring", ObjectType = ObjectType.BowString, Price = 400 },
        new ObjectInfo("O") { Number = ShopRepair.LightBowstringId, Name = "Light Bowstring", ObjectType = ObjectType.BowString, Price = 200 },
        new ObjectInfo("O") { Number = 71, Name = "A key", ObjectType = ObjectType.Key, Price = 50 },
    });

    [Theory]
    // The seven masks are the seven arms of dialog 1800038, and these are its own words.
    [InlineData(1, ObjectType.Sword, true)]      // "sharpens swords and such"
    [InlineData(1, ObjectType.Armor, false)]
    [InlineData(2, ObjectType.Armor, true)]      // "fixes armor and the like"
    [InlineData(2, ObjectType.Sword, false)]
    [InlineData(3, ObjectType.Sword, true)]      // "beats the dents out of armor and sharpens swords"
    [InlineData(3, ObjectType.Armor, true)]
    [InlineData(3, ObjectType.Crossbow, false)]
    [InlineData(4, ObjectType.Crossbow, true)]   // "repairs crossbows"
    [InlineData(7, ObjectType.Sword, true)]      // "can fix nearly anything"
    [InlineData(7, ObjectType.Armor, true)]
    [InlineData(7, ObjectType.Crossbow, true)]
    public void TheMaskSaysWhatHeWillTouch(int mask, ObjectType type, bool mends) =>
        Assert.Equal(mends, ShopRepair.Mends(mask, type));

    [Fact]
    public void NoMaskAdmitsAnythingBUTTheThreeWeaponKinds() {
        // A staff, a potion or a key is refused by the mender who "can fix nearly anything".
        Assert.False(ShopRepair.Mends(7, ObjectType.Staff));
        Assert.False(ShopRepair.Mends(7, ObjectType.Potion));
        Assert.False(ShopRepair.Mends(7, ObjectType.Key));
    }

    [Fact]
    public void ASwordIsPricedByHowWornItIs() {
        ObjectInfoSet objects = Objects();
        // 1000 * 100 * (100 - 60) / 10000
        Assert.Equal(400, ShopRepair.PriceFor(new RuntimeItem(Broadsword, 60, 0),
            objects.GetById(Broadsword), 100, objects.GetById));
        // ... and a markup scales the whole thing.
        Assert.Equal(800, ShopRepair.PriceFor(new RuntimeItem(Broadsword, 60, 0),
            objects.GetById(Broadsword), 200, objects.GetById));
    }

    [Fact]
    public void ANearlyPristineBladeIsNearlyFree() {
        ObjectInfoSet objects = Objects();
        // 1000 * 100 * 1 / 10000 = 10, and the integer division is the original's.
        Assert.Equal(10, ShopRepair.PriceFor(new RuntimeItem(Broadsword, 99, 0),
            objects.GetById(Broadsword), 100, objects.GetById));
    }

    [Fact]
    public void ACrossbowIsRESTRUNGAtAFlatPriceThatIgnoresItsCondition() {
        // *** THE ODD FORMULA. *** Twice the catalogue price of the bowstring it takes — nothing to
        // do with the crossbow's own price, its wear, or the shop's markup.
        ObjectInfoSet objects = Objects();
        foreach (byte condition in new byte[] { 1, 50, 99 }) {
            Assert.Equal(400, ShopRepair.PriceFor(new RuntimeItem(PlainCrossbow, condition, 0),
                objects.GetById(PlainCrossbow), 100, objects.GetById));
            Assert.Equal(400, ShopRepair.PriceFor(new RuntimeItem(PlainCrossbow, condition, 0),
                objects.GetById(PlainCrossbow), 500, objects.GetById));
        }
    }

    [Fact]
    public void BessyMaulerTakesTheHEAVYStringAndCostsTwiceAsMuch() {
        ObjectInfoSet objects = Objects();
        Assert.Equal(800, ShopRepair.PriceFor(
            new RuntimeItem(ShopRepair.BessyMaulerId, 50, 0),
            objects.GetById(ShopRepair.BessyMaulerId), 100, objects.GetById));
    }

    [Fact]
    public void TheCATEGORYIsTestedBeforeTheCondition() {
        // A pristine sword at an armour-only mender hears "he cannot mend that", not "it needs no
        // repair" — the original's two ifs are in that order.
        ObjectInfoSet objects = Objects();
        Assert.Equal(ShopRepair.Outcome.CannotMend, ShopRepair.OutcomeFor(
            new RuntimeItem(Broadsword, 100, 0), objects.GetById(Broadsword), repairCategories: 2));
        Assert.Equal(ShopRepair.Outcome.NeedsNoRepair, ShopRepair.OutcomeFor(
            new RuntimeItem(Broadsword, 100, 0), objects.GetById(Broadsword), repairCategories: 1));
        Assert.Equal(ShopRepair.Outcome.Quote, ShopRepair.OutcomeFor(
            new RuntimeItem(Broadsword, 99, 0), objects.GetById(Broadsword), repairCategories: 1));
    }

    [Fact]
    public void AMendedItemComesBackWholeAndNoLongerReadsAsDamaged() {
        var item = new RuntimeItem(Broadsword, 12, (ushort)(ItemFlags.Repairable | ItemFlags.Equipped));

        ShopRepair.Apply(item);

        Assert.Equal(InventoryQuery.PristineCondition, item.Variable);
        Assert.Equal(0, item.ItemFlags & (ushort)ItemFlags.Repairable);
        Assert.NotEqual(0, item.ItemFlags & (ushort)ItemFlags.Equipped);  // it stays worn
    }

    [Fact]
    public void ThreeSwordsCostTheSameFourHoursAsOne() {
        // *** OR, NOT SUM. *** `timeFlags |= di` then `while (timeFlags-- != 0)`, so the visit's
        // accumulated mask IS the hour count.
        int mask = 0;
        for (int i = 0; i < 3; i++) { mask |= ShopRepair.TimeBitFor(ObjectType.Sword); }

        Assert.Equal(4, ShopRepair.HoursFor(mask));
    }

    [Fact]
    public void ASwordAndAPieceOfArmourCostTWELVE() {
        int mask = ShopRepair.TimeBitFor(ObjectType.Sword) | ShopRepair.TimeBitFor(ObjectType.Armor);

        Assert.Equal(12, ShopRepair.HoursFor(mask));
    }

    [Fact]
    public void MendingNothingCostsNoTime() =>
        Assert.Equal(0, ShopRepair.HoursFor(0));

    [Fact]
    public void EachKindHasItsOwnHours() {
        Assert.Equal(4, ShopRepair.TimeBitFor(ObjectType.Sword));
        Assert.Equal(8, ShopRepair.TimeBitFor(ObjectType.Armor));
        Assert.Equal(2, ShopRepair.TimeBitFor(ObjectType.Crossbow));
        Assert.Equal(0, ShopRepair.TimeBitFor(ObjectType.Staff));
    }
}
