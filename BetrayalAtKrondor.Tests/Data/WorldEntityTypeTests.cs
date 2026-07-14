namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.World;
using Xunit;

public class WorldEntityTypeTests {
    [Theory]
    [InlineData(WorldEntityType.Container, 6)]
    [InlineData(WorldEntityType.Grave, 12)]
    [InlineData(WorldEntityType.WayMarker, 13)]
    [InlineData(WorldEntityType.Corpse, 16)]
    [InlineData(WorldEntityType.Door, 23)]
    [InlineData(WorldEntityType.Well, 31)]
    [InlineData(WorldEntityType.Ladder, 42)]
    public void ValuesMatchDosHandlerBytes(WorldEntityType t, byte expected) =>
        Assert.Equal(expected, (byte)t);
}
