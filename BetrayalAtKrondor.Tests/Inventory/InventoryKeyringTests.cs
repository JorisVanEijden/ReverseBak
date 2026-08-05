namespace BetrayalAtKrondor.Tests.Inventory;
using GameData;
using GameData.Resources.Data;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The cat-7 diversion (spec docs/specs/inventory-item-handling.md §4; pickupItem @0x558c2,
/// CMBINV.C:881-901): a key bound for a party member goes to the party's ONE shared keys
/// inventory instead, one slot per key kind with the count in <c>Variable</c>.
/// </summary>
public class InventoryKeyringTests {
    private const byte PeasantsKey = 61;
    private const byte VirtueKey = 62;
    private const byte Picklocks = 80;

    private static ObjectInfoSet Objs() => new ObjectInfoSet("O", new List<ObjectInfo> {
        new ObjectInfo("k1") { Number = PeasantsKey, Name = "Peasant's Key", InventorySlots = 1, ObjectType = ObjectType.Key },
        new ObjectInfo("k2") { Number = VirtueKey, Name = "Virtue Key", InventorySlots = 1, ObjectType = ObjectType.Key },
        new ObjectInfo("p") { Number = Picklocks, Name = "Picklocks", InventorySlots = 1, MaxAmount = 5 },
    });

    private static RuntimeContainer C(int cap, SaveGameContainerType type, params RuntimeItem[] items) {
        var c = new RuntimeContainer { Capacity = cap, ContainerType = type };
        c.Items.AddRange(items);
        return c;
    }

    private static RuntimeContainer Keyring(params RuntimeItem[] items) =>
        C(20, SaveGameContainerType.SharedKeys, items);

    [Fact]
    public void NewKind_LandsOnTheKeyring_WithCountOne_NotInTheMemberPack() {
        var src = C(4, SaveGameContainerType.Chest, new RuntimeItem(PeasantsKey, 0, 0));
        var member = C(24, SaveGameContainerType.Inventory);
        var keys = Keyring();
        int gold = 0;

        var r = InventoryTransfer.Move(src, 0, member, Objs(), ref gold, sharedKeys: keys);

        Assert.Equal(InventoryTransfer.Result.Moved, r);
        Assert.Empty(src.Items);
        Assert.Empty(member.Items);
        Assert.Single(keys.Items);
        Assert.Equal(PeasantsKey, keys.Items[0].ObjectId);
        Assert.Equal(1, keys.Items[0].Variable); // the picked-up key's own count is discarded
        Assert.True(keys.Dirty);
    }

    [Fact]
    public void KindAlreadyHeld_BumpsTheCount_WithoutAddingASlot() {
        var src = C(4, SaveGameContainerType.Chest, new RuntimeItem(PeasantsKey, 0, 0));
        var member = C(24, SaveGameContainerType.Inventory);
        var keys = Keyring(new RuntimeItem(PeasantsKey, 2, 0));
        int gold = 0;

        InventoryTransfer.Move(src, 0, member, Objs(), ref gold, sharedKeys: keys);

        Assert.Single(keys.Items);
        Assert.Equal(3, keys.Items[0].Variable);
    }

    [Fact]
    public void DifferentKinds_GetSeparateSlots() {
        var src = C(4, SaveGameContainerType.Chest, new RuntimeItem(VirtueKey, 0, 0));
        var member = C(24, SaveGameContainerType.Inventory);
        var keys = Keyring(new RuntimeItem(PeasantsKey, 1, 0));
        int gold = 0;

        InventoryTransfer.Move(src, 0, member, Objs(), ref gold, sharedKeys: keys);

        Assert.Equal(2, keys.Items.Count);
        Assert.Contains(keys.Items, i => i.ObjectId == PeasantsKey && i.Variable == 1);
        Assert.Contains(keys.Items, i => i.ObjectId == VirtueKey && i.Variable == 1);
    }

    [Fact]
    public void TheKeyringNeverRefuses_EvenWithNoRoomInTheMemberPack() {
        // A member at their count cap: the normal path would answer DoesNotFit. The keys branch
        // runs before any fit test, so the key still lands (no fit test on this path at all).
        var member = C(1, SaveGameContainerType.Inventory, new RuntimeItem(Picklocks, 1, 0));
        var src = C(4, SaveGameContainerType.Chest, new RuntimeItem(PeasantsKey, 0, 0));
        var keys = Keyring();
        int gold = 0;

        var r = InventoryTransfer.Move(src, 0, member, Objs(), ref gold, sharedKeys: keys);

        Assert.Equal(InventoryTransfer.Result.Moved, r);
        Assert.Single(keys.Items);
        Assert.Single(member.Items); // still just the picklocks
    }

    [Fact]
    public void KeyToANonMemberContainer_TakesTheNormalPath() {
        // The diversion is gated on the DESTINATION being a party member (RES_PARTY_SLOT):
        // dropping a key into a chest is an ordinary move.
        var member = C(24, SaveGameContainerType.Inventory, new RuntimeItem(PeasantsKey, 0, 0));
        var chest = C(10, SaveGameContainerType.Chest);
        var keys = Keyring();
        int gold = 0;

        var r = InventoryTransfer.Move(member, 0, chest, Objs(), ref gold, sharedKeys: keys);

        Assert.Equal(InventoryTransfer.Result.Moved, r);
        Assert.Empty(keys.Items);
        Assert.Single(chest.Items);
    }

    [Fact]
    public void NoKeyringAvailable_LeavesTheKeyOnTheNormalPath() {
        // A save with no shared-keys container must not silently drop the key.
        var src = C(4, SaveGameContainerType.Chest, new RuntimeItem(PeasantsKey, 0, 0));
        var member = C(24, SaveGameContainerType.Inventory);
        int gold = 0;

        var r = InventoryTransfer.Move(src, 0, member, Objs(), ref gold);

        Assert.Equal(InventoryTransfer.Result.Moved, r);
        Assert.Single(member.Items);
        Assert.Equal(PeasantsKey, member.Items[0].ObjectId);
    }

    [Fact]
    public void NonKeyBoundForAMember_IsUnaffectedByTheKeyring() {
        var src = C(4, SaveGameContainerType.Chest, new RuntimeItem(Picklocks, 3, 0));
        var member = C(24, SaveGameContainerType.Inventory);
        var keys = Keyring();
        int gold = 0;

        InventoryTransfer.Move(src, 0, member, Objs(), ref gold, sharedKeys: keys);

        Assert.Empty(keys.Items);
        Assert.Single(member.Items);
        Assert.Equal(3, member.Items[0].Variable);
    }
}
