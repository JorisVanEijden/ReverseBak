namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using GameData.Resources.Inventory;
using Xunit;

/// <summary>
/// Counting ammunition. The kind table is out of order and the quantity lives in a field that means
/// something else for ordinary goods — both are easy to get wrong.
/// </summary>
public class QuarrelInventoryTests {
    private static RuntimeContainer Pack(params RuntimeItem[] items) {
        var c = new RuntimeContainer();
        foreach (RuntimeItem i in items) {
            c.Items.Add(i);
        }
        return c;
    }

    private static RuntimeItem Item(int objectId, byte quantity) =>
        new RuntimeItem((byte)objectId, quantity, default);

    [Fact]
    public void TheKindTableIsNotInObjectIdOrder() {
        // *** The trap. *** 0x2a is kind 3 and 0x27 is kind 4 - a port that assumes 0x24..0x2b run
        // straight through swaps two kinds, and an archer's bolts count as the wrong type.
        Assert.Equal(3, QuarrelInventory.KindOf(0x2a));
        Assert.Equal(4, QuarrelInventory.KindOf(0x27));
        Assert.Equal(0, QuarrelInventory.KindOf(0x24));
        Assert.Equal(7, QuarrelInventory.KindOf(0x2b));
        Assert.Equal(-1, QuarrelInventory.KindOf(0x99));
        Assert.Equal(8, QuarrelInventory.ObjectIdByKind.Length);
    }

    [Fact]
    public void QuantityComesFromTheChargesField() {
        Assert.Equal(20, QuarrelInventory.Count(Pack(Item(0x24, 20)), kind: 0));
    }

    [Fact]
    public void AnEmptyQuiverAnswersZero_NotOne() {
        // *** Why this does not reuse InventoryQuery.CountByKind. *** That helper reads Variable == 0
        // as one item, which is right for ordinary goods and wrong here: it would hand an archer a
        // shot they cannot take.
        var empty = Pack(Item(0x24, 0));
        Assert.Equal(0, QuarrelInventory.Count(empty, kind: 0));
        Assert.Equal(1, InventoryQuery.CountByKind(empty, 0x24));
    }

    [Fact]
    public void TheTotalSumsEveryKind() {
        RuntimeContainer pack = Pack(Item(0x24, 5), Item(0x2a, 3), Item(0x2b, 2));
        Assert.Equal(10, QuarrelInventory.Count(pack));
        Assert.Equal(5, QuarrelInventory.Count(pack, kind: 0));
        Assert.Equal(3, QuarrelInventory.Count(pack, kind: 3));
    }

    [Fact]
    public void NonAmmunitionIsIgnored() {
        Assert.Equal(0, QuarrelInventory.Count(Pack(Item(0x10, 99))));
    }

    [Fact]
    public void StacksOfOneKindAddUp() {
        Assert.Equal(7, QuarrelInventory.Count(Pack(Item(0x25, 4), Item(0x25, 3)), kind: 1));
    }

    [Fact]
    public void AnAbsentPackIsZeroRatherThanAThrow() {
        Assert.Equal(0, QuarrelInventory.Count(null));
    }
}
