namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using System;
using Xunit;

/// <summary>
/// How a container names the member whose pack it is.
/// </summary>
/// <remarks>
/// The stored actor number is 1-based; the party record set is indexed from 0. Nothing in the
/// types makes the two hard to confuse, and confusing them handed every member the pack of the
/// member before them — with Locklear, actor 1, getting none at all.
/// </remarks>
public class ContainerOwnerNumberingTests {
    private static SaveGameContainerData Container(SaveGameContainerType type, short actorNumber) =>
        new SaveGameContainerData(
            new SaveGameContainerLocationData(0, 1, 9, 0, 0, 0, actorNumber),
            type, numberOfItems: 0, capacity: 24, dataTypes: 0,
            items: Array.Empty<SaveGameInventoryItemData>(),
            lockData: null, dialogData: null, shopData: null, encounterData: null,
            timestamp: null, globalStateIndex: null);

    [Fact]
    public void ActorOneIsTheFirstMember() {
        // canAutoEquipOnPickup @0x55414 indexes actors_Locklear[actorNr - 1].
        SaveGameContainerData c = Container(SaveGameContainerType.Inventory, actorNumber: 1);

        Assert.Equal((short)1, c.OwnerActorNumber);
        Assert.Equal(0, c.OwnerPartyPosition);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(6, 5)]
    public void PositionIsAlwaysOneBelowTheActorNumber(short actorNumber, int expectedPosition) =>
        Assert.Equal(expectedPosition,
            Container(SaveGameContainerType.Inventory, actorNumber).OwnerPartyPosition);

    [Fact]
    public void ActorZeroMeansNoOwner() {
        // 0 is the "not an actor's" marker, NOT Locklear — reading it as a position would make it
        // party slot -1.
        SaveGameContainerData c = Container(SaveGameContainerType.Inventory, actorNumber: 0);

        Assert.Null(c.OwnerActorNumber);
        Assert.Null(c.OwnerPartyPosition);
    }

    [Fact]
    public void OnlyAnInventoryContainerHasAnOwner() {
        // A chest's location record reuses the same field for something else, so the type guard is
        // what keeps a chest from claiming to be somebody's pack.
        SaveGameContainerData chest = Container(SaveGameContainerType.Chest, actorNumber: 3);

        Assert.Null(chest.OwnerActorNumber);
        Assert.Null(chest.OwnerPartyPosition);
    }

    [Fact]
    public void TheTwoNumberingsAreNotInterchangeable() =>
        // The point of the whole file: same container, two different correct answers.
        Assert.NotEqual(
            Container(SaveGameContainerType.Inventory, actorNumber: 4).OwnerActorNumber,
            (short?)Container(SaveGameContainerType.Inventory, actorNumber: 4).OwnerPartyPosition);
}
