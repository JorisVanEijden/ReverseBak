namespace BetrayalAtKrondor.Tests.Character;

using System.Collections.Generic;
using GameData;
using GameData.Resources.Data;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The two delegated cast routines read so far. Strength Drain turns out to be a transfer, and
/// Steelfire enchants somebody else's sword.
/// </summary>
public class SpellCastRoutinesTests {
    [Fact]
    public void TheDrainIsClampedToWhatTheTargetStillHas() {
        Assert.Equal(12, SpellCastRoutines.ActualDrain(requested: 20, targetCurrentStrength: 12));
        Assert.Equal(20, SpellCastRoutines.ActualDrain(requested: 20, targetCurrentStrength: 60));
    }

    [Fact]
    public void AndTheCasterBanksHalfOfWhatWasActuallyTaken() {
        // Not half of what was asked for — draining a spent target is nearly worthless.
        int actual = SpellCastRoutines.ActualDrain(requested: 20, targetCurrentStrength: 4);
        Assert.Equal(2, SpellCastRoutines.CasterGain(actual));
    }

    [Fact]
    public void AWindElementalAtOrBelowTheDrainDiesInstead() {
        Assert.True(SpellCastRoutines.DrainKillsOutright(
            SpellCastRoutines.WindElementalCreatureType, targetCurrentStrength: 8, drain: 20));
        Assert.True(SpellCastRoutines.DrainKillsOutright(
            SpellCastRoutines.WindElementalCreatureType, targetCurrentStrength: 20, drain: 20));
        Assert.False(SpellCastRoutines.DrainKillsOutright(
            SpellCastRoutines.WindElementalCreatureType, targetCurrentStrength: 21, drain: 20));
    }

    [Fact]
    public void AndNothingElseDoes() {
        Assert.False(SpellCastRoutines.DrainKillsOutright(
            creatureType: 12, targetCurrentStrength: 1, drain: 99));
    }

    [Fact]
    public void TheWindElementalIsAlsoImmuneToGrief() {
        // Corroborates the creature number read from the compare's bytes: 54 sits inside the band
        // Grief of 1000 Nights exempts.
        Assert.False(
            SpellPerSpellHandlers.GriefAffects(SpellCastRoutines.WindElementalCreatureType));
    }

    [Fact]
    public void AMonsterCasterBanksHalfWhatAPartyCasterDoes() {
        // The gain paths disagree on scale — 128 against the 256 the loss paths use.
        Assert.Equal(10, SpellCastRoutines.CasterGain(20));
        Assert.Equal(5, SpellCastRoutines.PermanentCasterGainPoints(20));
    }

    [Fact]
    public void SteelfireFindsTheFirstEquippedSword() {
        var objects = BuildObjects();
        var container = BuildContainer(
            (objectId: 30, flags: (ushort)ItemFlags.Equipped),      // armor, equipped
            (objectId: 20, flags: 0),                                // sword, not equipped
            (objectId: 20, flags: (ushort)ItemFlags.Equipped));      // sword, equipped

        Assert.Equal(2, SpellCastRoutines.SteelfireTarget(container, objects));
    }

    [Fact]
    public void AndFindsNothingWhenNoSwordIsWorn() {
        var objects = BuildObjects();
        var container = BuildContainer(
            (objectId: 30, flags: (ushort)ItemFlags.Equipped),
            (objectId: 20, flags: 0));

        Assert.Equal(-1, SpellCastRoutines.SteelfireTarget(container, objects));
    }

    [Fact]
    public void WhichStillCostsTheCaster() {
        Assert.True(SpellCastRoutines.SteelfireChargesEvenWhenItFindsNothing);
    }

    [Fact]
    public void TheEnchantmentIsOredInAndLeavesOtherFlagsAlone() {
        ushort before = (ushort)(ItemFlags.Equipped | ItemFlags.Flaming);
        ushort after = SpellCastRoutines.ApplySteelfire(before);

        Assert.Equal((ushort)ItemFlags.SteelFired, (ushort)(after & (ushort)ItemFlags.SteelFired));
        Assert.Equal((ushort)ItemFlags.Equipped, (ushort)(after & (ushort)ItemFlags.Equipped));
        Assert.Equal((ushort)ItemFlags.Flaming, (ushort)(after & (ushort)ItemFlags.Flaming));
    }

    [Fact]
    public void AndIsIdempotent() {
        ushort once = SpellCastRoutines.ApplySteelfire((ushort)ItemFlags.Equipped);
        Assert.Equal(once, SpellCastRoutines.ApplySteelfire(once));
    }

    [Fact]
    public void AnAbsentTargetIsNotAnError() {
        Assert.Equal(-1, SpellCastRoutines.SteelfireTarget(null, BuildObjects()));
    }

    private static ObjectInfoSet BuildObjects() => new ObjectInfoSet("O", new List<ObjectInfo> {
        new ObjectInfo("sw") {
            Number = 20, Name = "Broadsword", ObjectType = ObjectType.Sword,
            InventorySlots = 2, MaxAmount = 1,
        },
        new ObjectInfo("ar") {
            Number = 30, Name = "Armor", ObjectType = ObjectType.Armor,
            InventorySlots = 4, MaxAmount = 1,
        },
    });

    private static RuntimeContainer BuildContainer(params (byte objectId, ushort flags)[] items) {
        var container = new RuntimeContainer {
            Capacity = 24, ContainerType = SaveGameContainerType.Inventory,
        };
        foreach ((byte objectId, ushort flags) in items) {
            container.Items.Add(new RuntimeItem(objectId, 0, flags));
        }
        return container;
    }
}
