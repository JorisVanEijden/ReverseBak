namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Inventory;
using GameData.Resources.World;
using Xunit;

public class ContainerImageTests {
    [Fact] public void Container_IsChestImage1() =>
        Assert.Equal(1, ContainerImage.IndexForWorldEntityType(WorldEntityType.Container));

    [Fact] public void Corpse_IsDeadBodyImage4() =>
        Assert.Equal(4, ContainerImage.IndexForWorldEntityType(WorldEntityType.Corpse));

    [Fact] public void Grave_IsImage3() =>
        Assert.Equal(3, ContainerImage.IndexForWorldEntityType(WorldEntityType.Grave));

    [Fact] public void Dirt_IsImage2() =>
        Assert.Equal(2, ContainerImage.IndexForWorldEntityType(WorldEntityType.Dirt));

    [Fact] public void Building_IsImage10() =>
        Assert.Equal(10, ContainerImage.IndexForWorldEntityType(WorldEntityType.Building));

    [Fact] public void Crystals_IsImage8() =>
        Assert.Equal(8, ContainerImage.IndexForWorldEntityType(WorldEntityType.Crystals));

    [Fact] public void Bush_IsImage6() =>
        Assert.Equal(6, ContainerImage.IndexForWorldEntityType(WorldEntityType.Bush));

    [Fact] public void TreeStump_IsImage9() =>
        Assert.Equal(9, ContainerImage.IndexForWorldEntityType(WorldEntityType.TreeStump));

    [Fact] public void DeadAnimal_IsImage5() =>
        Assert.Equal(5, ContainerImage.IndexForWorldEntityType(WorldEntityType.DeadAnimal));

    // Terrain / unmapped types fall through to the original's default 0 (e.g. Well=31 → default).
    [Fact] public void Well_FallsThroughToDefault0() =>
        Assert.Equal(0, ContainerImage.IndexForWorldEntityType(WorldEntityType.Well));

    [Fact] public void WayMarker_FallsThroughToDefault0() =>
        Assert.Equal(0, ContainerImage.IndexForWorldEntityType(WorldEntityType.WayMarker));
}
