namespace BetrayalAtKrondor.Tests.Shop;

using GameData.Resources.Object;
using GameData.Resources.Shop;
using Xunit;

/// <summary>
/// The name line of a shop shelf cell.
/// </summary>
/// <remarks>
/// Measured against the original at Fletcher's Post, LaMut, on 2026-09-07 — the port showed the
/// bare name and the original shows the condition or count in the name, so nothing on a shelf said
/// how worn a weapon was or how many quarrels were in the stack.
/// </remarks>
public class ShopCellLabelTests {
    [Fact]
    public void ADegradableItemShowsItsConditionAsAPercentage() {
        // "Broadsword (100%)" — flags 'B0008, Degradable', i.e. 0x1000 | 0x8.
        Assert.Equal("Broadsword (100%)",
            ShopCellLabel.LinesFor("Broadsword", 0, (ObjectFlags)0x1008, 100).Last);
    }

    [Fact]
    public void AStackShowsItsCountWithoutAPercentSign() {
        // "Quarrels (25)" — flags 'Stackable, B8000'.
        Assert.Equal("Quarrels (25)", ShopCellLabel.LinesFor("Quarrels", 0, (ObjectFlags)0x8000, 25).Last);
    }

    [Fact]
    public void TheCountArmIsTakenForEitherBitOfTheMask_NotJust0x8000() {
        // *** THE MASK IS 0xa000, NOT 0x8000. *** INVENTOR.C:465 tests both bits, so a 0x2000 item
        // takes the count arm too. Reading the corner label's rule across (0x8000, else 0x3000
        // select-only) gets Quarrels right by luck and this case wrong.
        Assert.Equal("Thing (7)", ShopCellLabel.LinesFor("Thing", 0, (ObjectFlags)0x2000, 7).Last);
    }

    [Fact]
    public void TheCountArmWinsWhenAnItemIsBothCountedAndDegradable() {
        // The original's arms are if/else in this order, so 0xa000 decides before 0x1000 is looked
        // at. Swapping them would print a percent sign on a stack.
        Assert.Equal("Both (3)", ShopCellLabel.LinesFor("Both", 0, (ObjectFlags)(0x8000 | 0x1000), 3).Last);
    }

    [Fact]
    public void AnItemWithNeitherFlagIsJustItsName() {
        Assert.Equal("Plain", ShopCellLabel.LinesFor("Plain", 0, (ObjectFlags)0, 42).Last);
    }

    [Fact]
    public void ThePercentArmDoesNotNeedTheGridsExtraBit() {
        // The corner label wants 0x1000 AND 0x8; the shelf wants only 0x1000. An item with the
        // first and not the second still shows its condition here.
        Assert.Equal("Worn (60%)", ShopCellLabel.LinesFor("Worn", 0, (ObjectFlags)0x1000, 60).Last);
    }

    [Fact]
    public void ALongNameIsSplitAtItsAuthoredPoint_AndTheSuffixRidesTheSECONDLine() {
        // "Standard Kingdom Armor" ships WordWrap 16, and the original renders it as two lines with
        // the condition on the lower one. Keeping it on one line is what ran it into the next
        // cell's text; the original never wraps, so the authored split IS the fit.
        (string first, string last) =
            ShopCellLabel.LinesFor("Standard Kingdom Armor", 16, (ObjectFlags)0x1008, 100);

        Assert.Equal("Standard Kingdom", first);
        Assert.Equal("Armor (100%)", last);
    }

    [Fact]
    public void TheSplitCharacterItselfIsDropped_NotKeptOnEitherLine() {
        // INVENTOR.C nul-terminates AT the index and restarts at index+1, so the space at the split
        // belongs to neither line. An off-by-one here shows as a leading space on the lower line.
        (string first, string last) =
            ShopCellLabel.LinesFor("Tsurani Light Crossbow", 13, (ObjectFlags)0x1008, 100);

        Assert.Equal("Tsurani Light", first);
        Assert.Equal("Crossbow (100%)", last);
    }

    [Fact]
    public void AShortNameHasNoFirstLineAtAll() {
        // WordWrap 0 is the "fits on one line" encoding, not "split at the start".
        Assert.Null(ShopCellLabel.LinesFor("Broadsword", 0, (ObjectFlags)0x1008, 100).First);
    }

    [Fact]
    public void AnOutOfRangeSplitIsIgnoredRatherThanThrowing() {
        // Defensive: an override author can ship a WordWrap past the end of a renamed item, and a
        // shelf that throws takes the whole screen with it.
        (string first, string last) = ShopCellLabel.LinesFor("Short", 99, (ObjectFlags)0, 1);

        Assert.Null(first);
        Assert.Equal("Short", last);
    }
}
