namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using System;
using Xunit;

public class ContainerLocatorTests {
    private static SaveGameContainerData Container(int zone, int x, int y, int minCh, int maxCh, SaveGameContainerType type) =>
        new SaveGameContainerData(
            new SaveGameContainerLocationData(zone, minCh, maxCh, worldItemId: 195, x: x, y: y, actorNumber: 0),
            type, numberOfItems: 0, capacity: 0, dataTypes: 0,
            items: Array.Empty<SaveGameInventoryItemData>(),
            lockData: null, dialogData: null, shopData: null, encounterData: null,
            timestamp: null, globalStateIndex: null);

    private static SaveGameZoneContainerStateData State(params SaveGameContainerData[] zone1) =>
        new SaveGameZoneContainerStateData(new[] {
            new SaveGameZoneContainerEntryData(zoneNumber: 1, tempGamFileOffset: 0, containers: zone1),
        });

    [Fact]
    public void FindsExactCoordMatchInChapterRange() {
        var corpse = Container(1, 670423, 1059778, minCh: 1, maxCh: 9, SaveGameContainerType.FixedWorldItem);
        var state = State(corpse);

        SaveGameContainerData? hit = ContainerLocator.FindContainerAtLocation(state, zone: 1, x: 670423, y: 1059778, chapter: 1);

        Assert.Same(corpse, hit);
    }

    [Fact]
    public void ReturnsNullWhenCoordsDiffer() {
        var state = State(Container(1, 670423, 1059778, 1, 9, SaveGameContainerType.FixedWorldItem));
        Assert.Null(ContainerLocator.FindContainerAtLocation(state, 1, 670423, 1059779, 1));
    }

    [Fact]
    public void ReturnsNullWhenChapterOutOfRange() {
        var state = State(Container(1, 670423, 1059778, minCh: 3, maxCh: 5, SaveGameContainerType.FixedWorldItem));
        Assert.Null(ContainerLocator.FindContainerAtLocation(state, 1, 670423, 1059778, chapter: 1));
    }

    [Fact]
    public void ReturnsNullWhenZoneMissing() {
        var state = State(Container(1, 670423, 1059778, 1, 9, SaveGameContainerType.FixedWorldItem));
        Assert.Null(ContainerLocator.FindContainerAtLocation(state, zone: 2, x: 670423, y: 1059778, chapter: 1));
    }
}
