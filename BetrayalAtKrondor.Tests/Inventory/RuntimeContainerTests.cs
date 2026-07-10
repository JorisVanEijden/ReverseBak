namespace BetrayalAtKrondor.Tests.Inventory;
using GameData.Resources.Data;
using GameData.Resources.Inventory;
using System;
using Xunit;

public class RuntimeContainerTests {
    [Fact] public void FromSnapshot_CopiesItemsAndCapacity() {
        var snap = new SaveGameContainerData(
            new SaveGameContainerLocationData(1, 1, 9, 195, 670423, 1059778, 0),
            (SaveGameContainerType)5, numberOfItems: 2, capacity: 4, dataTypes: 0,
            items: new[] { new SaveGameInventoryItemData(80, 2, 0), new SaveGameInventoryItemData(72, 4, 0) },
            null, null, null, null, null, null);
        var rc = RuntimeContainer.FromSnapshot(snap);
        Assert.Equal(4, rc.Capacity);
        Assert.Equal(2, rc.Items.Count);
        Assert.Equal(80, rc.Items[0].ObjectId);
        Assert.False(rc.Dirty);
    }
}
