namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.Data;
using GameData.Resources.World;
using Xunit;

/// <summary>
/// Clicking a building or gate in the world — <c>wcursor_click_fixedobj_full</c>.
/// </summary>
public class FixedObjectClickTests {
    [Fact]
    public void ATRAPPEDObjectIsOnlyClickableFromThePartysOWNTile() {
        // The original returns before the sound and before any dialog, so clicking a town gate from
        // the next tile along produces NOTHING — no message, no click. Answering "you are too far
        // away" is more helpful than the original and changes what the silence teaches.
        Assert.True(FixedObjectClick.IsWithinReach(firesTrapEncounter: true, 4, 7, 4, 7));
        Assert.False(FixedObjectClick.IsWithinReach(firesTrapEncounter: true, 4, 7, 5, 7));
    }

    [Fact]
    public void AnObjectThatFiresNoTrapHasNoReachRestriction() {
        Assert.True(FixedObjectClick.IsWithinReach(firesTrapEncounter: false, 4, 7, 99, 99));
    }

    /// <summary>
    /// THE FENCE for the bug this parameter's old name caused. An object that CARRIES encounter
    /// data but fires no trap is clickable from anywhere.
    /// </summary>
    /// <remarks>
    /// The gate used to be handed "the encounter subrecord exists", which pinned 28 of the 96
    /// encounter-bearing containers to the party's own tile. WCURSOR.C:285 tests the subrecord AND
    /// <c>bHas_hotspot</c>, the byte inside it that IDA names <c>firesTrapEncounter</c> — and
    /// <c>handle_Grave</c> @0x77d5b tests the same byte the same way. canassa's name is the
    /// misleading half: it reads as "there is a hotspot" when it is a field within one.
    /// </remarks>
    [Fact]
    public void AnEncounterRecordAloneDoesNOTRestrictReach() {
        var firesNothing = new SaveGameContainerEncounterData(
            globalDataKey1: 0, globalDataKey2: 0, gdsNumber: 3, gdsLetter: 1,
            firesTrapEncounter: 0, x: 0, y: 0);

        Assert.False(firesNothing.IsFiresTrapEncounterSet);
        Assert.True(FixedObjectClick.IsWithinReach(
            firesNothing.IsFiresTrapEncounterSet, 4, 7, 99, 99),
            "a record with the flag clear must not pin the object to the party's tile");
    }

    [Fact]
    public void TheGateBitReadsBACKWARDSFromItsName() {
        // The original tests !(flags & 1) and calls the result "flag 1 clear": SET means gated.
        Assert.True(FixedObjectClick.GatePasses(flags: 0, eventValue: 0));
        Assert.False(FixedObjectClick.GatePasses(
            flags: FixedObjectClick.GatedOnEventFlag, eventValue: 0));
        Assert.True(FixedObjectClick.GatePasses(
            flags: FixedObjectClick.GatedOnEventFlag, eventValue: 1));
    }

    [Fact]
    public void TheGateArgumentIsPublishedEITHERWay() {
        // Set before the message plays, so a gated object can say something different while shut.
        Assert.Equal(0, FixedObjectClick.GateArgument(flags: 0, eventValue: 0));
        Assert.Equal(1, FixedObjectClick.GateArgument(
            flags: FixedObjectClick.GatedOnEventFlag, eventValue: 0));
    }

    [Fact]
    public void ALOCKEDObjectNeverLeadsAnywhere() {
        // The warp sits only on the unlocked branch, so a lock key makes it a container whatever
        // else it carries. Running the lock and falling through to the warp turns every locked
        // chest into a door.
        Assert.False(FixedObjectClick.CanEnterTownScene(lockKey: 12));
        Assert.Equal(FixedObjectClick.Outcome.Locked,
            FixedObjectClick.Resolve(lockKey: 12, hasMessage: true, hasWarp: true,
                flags: 0, eventValue: 0));
    }

    [Fact]
    public void AnUnlockedWarpObjectEntersTheTownScene() {
        Assert.Equal(FixedObjectClick.Outcome.EntersTownScene,
            FixedObjectClick.Resolve(lockKey: 0, hasMessage: true, hasWarp: true,
                flags: 0, eventValue: 0));
    }

    [Fact]
    public void AGatedDoorStaysShutUntilItsEventIsSet() {
        Assert.Equal(FixedObjectClick.Outcome.Refused,
            FixedObjectClick.Resolve(lockKey: 0, hasMessage: true, hasWarp: true,
                flags: FixedObjectClick.GatedOnEventFlag, eventValue: 0));
        Assert.Equal(FixedObjectClick.Outcome.EntersTownScene,
            FixedObjectClick.Resolve(lockKey: 0, hasMessage: true, hasWarp: true,
                flags: FixedObjectClick.GatedOnEventFlag, eventValue: 1));
    }

    [Fact]
    public void NoMessageIsNothingToDoWhateverElseItCarries() {
        Assert.Equal(FixedObjectClick.Outcome.NothingToDo,
            FixedObjectClick.Resolve(lockKey: 0, hasMessage: false, hasWarp: true,
                flags: 0, eventValue: 0));
    }

    [Fact]
    public void ThePackedWarpIsUnpackedByTheGDSLayerAndNotHere() {
        // townscene_main_loop unpacks a high byte over the index it was given — the same rule
        // GDS_RunScene applies to every scene reference, and GdsSceneRules already owns it. Two
        // copies of one packing convention would be free to drift.
        Assert.True(FixedObjectClick.WarpIsUnpackedByTheGdsLayer);
        Assert.Equal((7, 3), GameData.Resources.Scene.GdsSceneRules.UnpackScene(0x0307, 9));
    }

    [Fact]
    public void TheInventoryBitOnlyAppliesWhenThereIsNoWarp() {
        // A warp wins: the original returns into the town scene before it ever tests bit 1.
        Assert.Equal(FixedObjectClick.Outcome.EntersTownScene,
            FixedObjectClick.Resolve(lockKey: 0, hasMessage: true, hasWarp: true,
                flags: FixedObjectClick.OpensInventoryFlag, eventValue: 0));
        Assert.Equal(FixedObjectClick.Outcome.OpensInventory,
            FixedObjectClick.Resolve(lockKey: 0, hasMessage: true, hasWarp: false,
                flags: FixedObjectClick.OpensInventoryFlag, eventValue: 0));
    }
}
