namespace BetrayalAtKrondor.Tests.Spells;

using GameData.Resources.Spells;
using GameData.Resources.World;
using System;
using System.Linq;
using Xunit;

/// <summary>
/// The three locator searches — the REQ_CMAP marker passes behind <c>CastLocatorSpell</c>.
/// </summary>
public class LocatorMapTests {
    [Fact]
    public void TheFoodSearchIsTheOnlyOneThatTakesBushesAndCarcasses() {
        // This is what settles a naming IDA had flagged as an unverified guess: the food pass is the
        // only one whose type set contains the things food comes from — and it then asks HasFood.
        WorldEntityType[] food = LocatorMap.MarkedTypesFor(FieldSpells.LocatorTarget.Food);
        Assert.Contains(WorldEntityType.Bush, food);
        Assert.Contains(WorldEntityType.BushPoison, food);
        Assert.Contains(WorldEntityType.BushHealing, food);
        Assert.Contains(WorldEntityType.DeadAnimal, food);

        WorldEntityType[] valuables = LocatorMap.MarkedTypesFor(FieldSpells.LocatorTarget.Valuables);
        Assert.DoesNotContain(WorldEntityType.Bush, valuables);
        Assert.DoesNotContain(WorldEntityType.DeadAnimal, valuables);
        Assert.DoesNotContain(WorldEntityType.Corpse, valuables);
    }

    [Fact]
    public void TheMagicSearchIsTheOnlyOneThatTakesRiftMachinesAndCrystals() {
        WorldEntityType[] magic = LocatorMap.MarkedTypesFor(FieldSpells.LocatorTarget.Magic);
        Assert.Contains(WorldEntityType.RiftMachine, magic);
        Assert.Contains(WorldEntityType.Crystals, magic);

        foreach (FieldSpells.LocatorTarget other in new[] {
            FieldSpells.LocatorTarget.Valuables, FieldSpells.LocatorTarget.Food }) {
            Assert.DoesNotContain(WorldEntityType.RiftMachine, LocatorMap.MarkedTypesFor(other));
            Assert.DoesNotContain(WorldEntityType.Crystals, LocatorMap.MarkedTypesFor(other));
        }
    }

    [Fact]
    public void THEVALUABLESSearchAsksNothingAboutContents() {
        // It marks a chest whether or not there is anything in it — places worth opening, not
        // confirmed treasure. The other two only mark what actually holds the thing.
        Assert.False(LocatorMap.ChecksContents(FieldSpells.LocatorTarget.Valuables));
        Assert.True(LocatorMap.Marks(FieldSpells.LocatorTarget.Valuables, WorldEntityType.Container,
            groundDistance: 1000, extent: 0, holdsFood: false, holdsMagic: false));

        Assert.True(LocatorMap.ChecksContents(FieldSpells.LocatorTarget.Food));
        Assert.False(LocatorMap.Marks(FieldSpells.LocatorTarget.Food, WorldEntityType.Container,
            groundDistance: 1000, extent: 0, holdsFood: false, holdsMagic: true));
        Assert.True(LocatorMap.Marks(FieldSpells.LocatorTarget.Food, WorldEntityType.Container,
            groundDistance: 1000, extent: 0, holdsFood: true, holdsMagic: false));
    }

    [Fact]
    public void THREEKindsAreMagicalWithoutBeingAsked() {
        // Tested by type immediately before the HasMagic call, and they skip it.
        Assert.True(LocatorMap.Marks(FieldSpells.LocatorTarget.Magic, WorldEntityType.RiftMachine,
            groundDistance: 1000, extent: 0, holdsFood: false, holdsMagic: false));
        Assert.True(LocatorMap.Marks(FieldSpells.LocatorTarget.Magic, WorldEntityType.Pillar,
            groundDistance: 1000, extent: 0, holdsFood: false, holdsMagic: false));
        Assert.True(LocatorMap.Marks(FieldSpells.LocatorTarget.Magic, WorldEntityType.StoneSlab,
            groundDistance: 1000, extent: 0, holdsFood: false, holdsMagic: false));

        // An ordinary container still has to hold something magical.
        Assert.False(LocatorMap.Marks(FieldSpells.LocatorTarget.Magic, WorldEntityType.Container,
            groundDistance: 1000, extent: 0, holdsFood: false, holdsMagic: false));
    }

    [Fact]
    public void TheRangeIsONETileAndABigThingIsFoundFromFurtherOut() {
        // distance - the type's own extent < 64000. Dropping the extent term would hide large
        // objects whose centre is just over a tile away but whose edge is at the party's feet.
        Assert.True(LocatorMap.Marks(FieldSpells.LocatorTarget.Valuables, WorldEntityType.Building,
            groundDistance: 63999, extent: 0, holdsFood: false, holdsMagic: false));
        Assert.False(LocatorMap.Marks(FieldSpells.LocatorTarget.Valuables, WorldEntityType.Building,
            groundDistance: 64000, extent: 0, holdsFood: false, holdsMagic: false));
        Assert.True(LocatorMap.Marks(FieldSpells.LocatorTarget.Valuables, WorldEntityType.Building,
            groundDistance: 70000, extent: 10000, holdsFood: false, holdsMagic: false));
    }

    [Fact]
    public void AKindOutsideTheSearchIsNeverMarkedHoweverCloseItIs() {
        Assert.False(LocatorMap.Marks(FieldSpells.LocatorTarget.Valuables, WorldEntityType.Ladder,
            groundDistance: 0, extent: 0, holdsFood: true, holdsMagic: true));
        Assert.False(LocatorMap.Marks(FieldSpells.LocatorTarget.None, WorldEntityType.Container,
            groundDistance: 0, extent: 0, holdsFood: true, holdsMagic: true));
    }

    [Fact]
    public void AllThreeShareTheContainersAndTheFurnitureThatHoldsThings() {
        WorldEntityType[] shared = { WorldEntityType.Container, WorldEntityType.Building,
            WorldEntityType.Dirt, WorldEntityType.TreeStump, WorldEntityType.Well,
            WorldEntityType.SiegeEngine, WorldEntityType.Bag };
        foreach (FieldSpells.LocatorTarget target in new[] { FieldSpells.LocatorTarget.Valuables,
            FieldSpells.LocatorTarget.Food, FieldSpells.LocatorTarget.Magic }) {
            WorldEntityType[] set = LocatorMap.MarkedTypesFor(target);
            Assert.All(shared, t => Assert.Contains(t, set));
            // No set repeats a kind — they are read off jump tables, where a duplicate would mean a
            // misread case list.
            Assert.Equal(set.Length, set.Distinct().Count());
        }
    }

    [Fact]
    public void ItOpensAtTheZonesMAXIMUMZoomWhateverTheMapWasLeftAt() {
        Assert.True(LocatorMap.OpensAtMaximumZoom);
        // And the inset it draws into is the one the spell installs, not the travel viewport.
        Assert.Equal((134, 16, 167, 89), FieldSpells.LocatorViewport);
    }

    [Fact]
    public void TheClipRectIsTheInsetsOwnEdges() {
        // The original writes the clip edges separately from the viewport rect (134,16)-(300,104);
        // they are the same rectangle, so a port that keeps one and forgets the other is drawing
        // markers outside the hole they belong in.
        (int x, int y, int width, int height) = FieldSpells.LocatorViewport;
        Assert.Equal(300, x + width - 1);
        Assert.Equal(104, y + height - 1);
    }

    [Fact]
    public void TheMarkersTurnWithTheMap() {
        Assert.Equal(0x2000, LocatorMap.MarkersDrawnWithYaw(0x2000, northUp: false));
        Assert.Equal(0, LocatorMap.MarkersDrawnWithYaw(0x2000, northUp: true));
    }
}
