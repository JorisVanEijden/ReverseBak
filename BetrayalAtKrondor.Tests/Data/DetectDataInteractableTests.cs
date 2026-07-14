namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Config;
using GameData.Resources.World;
using Xunit;

public class DetectDataInteractableTests {
    private static DetectData Make() {
        var d = new DetectData("DETECT.DAT");
        var above = new DetectLocationRanges { Location = "Aboveground" };
        var under = new DetectLocationRanges { Location = "Underground" };
        above.DetectRanges[(byte)WorldEntityType.Corpse] = 16000;   // interactable above
        above.DetectRanges[(byte)WorldEntityType.Container] = 7000;
        // Door: 0 above / 2500 under (location-dependent)
        under.DetectRanges[(byte)WorldEntityType.Door] = 2500;
        d.Locations.Add(above);
        d.Locations.Add(under);
        return d;
    }

    [Fact] public void CorpseInteractableAboveground() =>
        Assert.True(Make().IsInteractable(WorldEntityType.Corpse, underground: false));

    [Fact] public void ContainerInteractableAboveground() =>
        Assert.True(Make().IsInteractable(WorldEntityType.Container, underground: false));

    [Fact] public void DoorNotInteractableAboveground_ButIsUnderground() {
        var d = Make();
        Assert.False(d.IsInteractable(WorldEntityType.Door, underground: false));
        Assert.True(d.IsInteractable(WorldEntityType.Door, underground: true));
    }

    [Fact] public void DecorativeTypeNeverInteractable() =>
        Assert.False(Make().IsInteractable((WorldEntityType)3, underground: false));
}
