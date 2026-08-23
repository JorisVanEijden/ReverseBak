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

    [Fact]
    public void GetRangeYieldsTheDistance_NotJustAYesNo() {
        // WorldEntityBuilder stamps this number onto the entity; IsInteractable only asks whether
        // it is non-zero. Both go through GetRange now, so they cannot disagree about a type.
        var d = Make();
        Assert.Equal(16000, d.GetRange(WorldEntityType.Corpse, underground: false));
        Assert.Equal(2500, d.GetRange(WorldEntityType.Door, underground: true));
        Assert.Equal(0, d.GetRange(WorldEntityType.Door, underground: false));
        Assert.Equal(d.IsInteractable(WorldEntityType.Door, underground: true),
            d.GetRange(WorldEntityType.Door, underground: true) > 0);
    }

    [Fact]
    public void AMissingLocationBlockReadsZeroRatherThanThrowing() {
        // The bounds checks the duplicate lookup in WorldEntityBuilder used to carry: one location
        // block present, and the other asked for.
        var d = new DetectData("DETECT.DAT");
        d.Locations.Add(new DetectLocationRanges { Location = "Aboveground" });

        Assert.Equal(0, d.GetRange(WorldEntityType.Container, underground: true));
        Assert.False(d.IsInteractable(WorldEntityType.Container, underground: true));
    }
}
