namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using Xunit;

public class SaveGameContainerTypeTests {
    [Theory]
    [InlineData(SaveGameContainerType.Free, 0)]
    [InlineData(SaveGameContainerType.Inventory, 1)]
    [InlineData(SaveGameContainerType.Bag, 2)]
    [InlineData(SaveGameContainerType.Chest, 4)]
    [InlineData(SaveGameContainerType.Corpse, 5)]
    [InlineData(SaveGameContainerType.FixedWorldItem, 6)]
    [InlineData(SaveGameContainerType.NpcInventory, 7)]
    [InlineData(SaveGameContainerType.SharedKeys, 8)]
    [InlineData(SaveGameContainerType.ScriptedLoot, 9)]
    public void EnumValuesMatchGameData(SaveGameContainerType t, byte expected) =>
        Assert.Equal(expected, (byte)t);
}
