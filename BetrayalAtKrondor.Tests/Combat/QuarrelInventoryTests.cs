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

    [Fact]
    public void TheScanTakesTheBestKindItHas_NotTheFirstOne() {
        // *** The rule this pins. *** Kind 7 (Enchanted) and kind 0 (plain) in the same pack: the
        // archer fires the enchanted one. Scanning 0..7 instead would hoard the good quarrels for
        // a fight that never comes.
        RuntimeContainer pack = Pack(Item(0x24, 10), Item(0x2b, 2));
        Assert.Equal(7, QuarrelInventory.Pick(pack, creatureType: 1, spend: false));
    }

    [Fact]
    public void TheScanFallsPastKindsTheArcherIsOutOf() {
        // An entry that exists but is empty must not win the scan — Variable holds the count.
        RuntimeContainer pack = Pack(Item(0x2b, 0), Item(0x27, 3));
        Assert.Equal(4, QuarrelInventory.Pick(pack, creatureType: 1, spend: false));
    }

    [Fact]
    public void PickingSpendsOne() {
        RuntimeContainer pack = Pack(Item(0x25, 3));
        Assert.Equal(1, QuarrelInventory.Pick(pack, creatureType: 1));
        Assert.Equal(2, QuarrelInventory.Count(pack, kind: 1));
    }

    [Fact]
    public void TheLastQuarrelLeavesTheStackBehind() {
        RuntimeContainer pack = Pack(Item(0x25, 1));
        Assert.Equal(1, QuarrelInventory.Pick(pack, creatureType: 1));
        Assert.Equal(0, QuarrelInventory.Count(pack));
        // ... and the next shot finds nothing.
        Assert.Equal(QuarrelInventory.NoQuarrel, QuarrelInventory.Pick(pack, creatureType: 1));
    }

    [Fact]
    public void NotSpendingLeavesThePackAlone() {
        RuntimeContainer pack = Pack(Item(0x25, 3));
        QuarrelInventory.Pick(pack, creatureType: 1, spend: false);
        Assert.Equal(3, QuarrelInventory.Count(pack, kind: 1));
    }

    [Fact]
    public void AnEmptyPackFiresNothing() {
        Assert.Equal(QuarrelInventory.NoQuarrel,
            QuarrelInventory.Pick(Pack(), creatureType: 1));
        Assert.Equal(QuarrelInventory.NoQuarrel,
            QuarrelInventory.Pick(Pack(Item(0x10, 99)), creatureType: 1));
    }

    [Fact]
    public void AnInsistedKindIsHonoured_AndRefusedWhenAbsent() {
        RuntimeContainer pack = Pack(Item(0x24, 10), Item(0x2b, 2));
        // Insisting on plain quarrels overrides the best-first scan.
        Assert.Equal(0, QuarrelInventory.Pick(pack, 1, preferredKind: 0, spend: false));
        // Insisting on a kind the pack lacks is not silently downgraded to another kind.
        Assert.Equal(QuarrelInventory.NoQuarrel,
            QuarrelInventory.Pick(pack, 1, preferredKind: 3, spend: false));
    }

    [Fact]
    public void KindNineCreatureShootsWithoutOwningAnything() {
        // *** creature 26 returns before the pack is ever looked at. *** Its kind is outside the
        // eight-entry table on purpose, so a port that indexes ObjectIdByKind with the result
        // throws instead of shooting.
        RuntimeContainer empty = Pack();
        Assert.Equal(QuarrelInventory.FreeAmmoKind,
            QuarrelInventory.Pick(empty, QuarrelInventory.FreeAmmoCreatureType));
        Assert.True(QuarrelInventory.FreeAmmoKind >= QuarrelInventory.ObjectIdByKind.Length);

        // And it does not raid a pack it happens to have.
        RuntimeContainer stocked = Pack(Item(0x2b, 2));
        Assert.Equal(QuarrelInventory.FreeAmmoKind,
            QuarrelInventory.Pick(stocked, QuarrelInventory.FreeAmmoCreatureType));
        Assert.Equal(2, QuarrelInventory.Count(stocked, kind: 7));
    }
}
