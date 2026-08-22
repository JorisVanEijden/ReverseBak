namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Finding usable equipment. Three rules here are counter-intuitive: the category-1 alias, the
/// untracked-condition default, and Broken overriding both.
/// </summary>
public class EquippedGearTests {
    private static EquippedGear.Slot Slot(int category, int condition = 50, bool equipped = true) =>
        new EquippedGear.Slot(equipped, category, condition);

    [Fact]
    public void AskingForMeleeAlsoAcceptsCategoryThree_ButRangedDoesNot() {
        // *** Asymmetric on purpose. *** altcategory = (category == 1) ? 3 : category. Applying the
        // alias symmetrically would let a category-3 item answer a RANGED query and offer a shot
        // with the wrong weapon.
        Assert.Equal(3, EquippedGear.AlternateCategoryFor(1));
        Assert.Equal(2, EquippedGear.AlternateCategoryFor(2));
        Assert.Equal(3, EquippedGear.AlternateCategoryFor(3));

        Assert.True(EquippedGear.HasIntact(new[] { Slot(3) }, category: 1), "3 answers a melee query");
        Assert.False(EquippedGear.HasIntact(new[] { Slot(3) }, category: 2), "but never a ranged one");
    }

    [Fact]
    public void AnItemTypeThatDoesNotTrackWearIsAlwaysAsNew() {
        // The original writes 'd' - 100. Reading it as 0 would make every simple weapon broken.
        Assert.Equal(100, EquippedGear.ConditionOf(false, typeTracksCondition: false, slotCondition: 0));
        Assert.Equal(EquippedGear.UntrackedCondition, 100);
    }

    [Fact]
    public void BrokenBeatsEverything() {
        // Applied AFTER the type test, so a broken item reads 0 even when its type does not track
        // condition and would otherwise report 100.
        Assert.Equal(0, EquippedGear.ConditionOf(isBroken: true, typeTracksCondition: false, slotCondition: 99));
        Assert.Equal(0, EquippedGear.ConditionOf(isBroken: true, typeTracksCondition: true, slotCondition: 99));
    }

    [Fact]
    public void ATrackedItemReportsItsOwnCondition() {
        Assert.Equal(7, EquippedGear.ConditionOf(false, typeTracksCondition: true, slotCondition: 7));
    }

    [Fact]
    public void IntactMeansAboveZero_NotAboveAUsableFloor() {
        // A weapon worn down to 1 still counts; only 0 fails.
        Assert.True(EquippedGear.HasIntact(new[] { Slot(2, condition: 1) }, category: 2));
        Assert.False(EquippedGear.HasIntact(new[] { Slot(2, condition: 0) }, category: 2));
    }

    [Fact]
    public void UnequippedGearIsNeverFound() {
        // A crossbow in the pack is not a crossbow in hand.
        Assert.False(EquippedGear.HasIntact(new[] { Slot(2, equipped: false) }, category: 2));
    }

    [Fact]
    public void TheWrongCategoryDoesNotAnswer() {
        Assert.False(EquippedGear.HasIntact(new[] { Slot(1) }, category: 2));
    }

    [Fact]
    public void AnEmptyOrAbsentLoadoutIsFalseRatherThanAThrow() {
        Assert.False(EquippedGear.HasIntact(null, 2));
        Assert.False(EquippedGear.HasIntact(new EquippedGear.Slot[0], 2));
    }
}
