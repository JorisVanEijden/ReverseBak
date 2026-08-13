namespace BetrayalAtKrondor.Tests.Data;

using GameData;
using GameData.Resources.Data;
using Xunit;

/// <summary>
/// Naming the per-placement properties doors and ladders read. The point of these is the mapping:
/// the container record's optional blocks ARE the actor subrecords, and the 4-byte params block is
/// a union our field names only describe one view of.
/// </summary>
public class FixedObjectAccessTests {
    private static SaveGameContainerData Object(
        SaveGameContainerLockData? lockData = null,
        SaveGameContainerDialogData? dialogData = null,
        short? globalStateIndex = null) =>
        new SaveGameContainerData(
            new SaveGameContainerLocationData(1, 1, 9, 1, 100, 200, 0),
            SaveGameContainerType.FixedWorldItem,
            numberOfItems: 0,
            capacity: 0,
            dataTypes: 0,
            items: new SaveGameInventoryItemData[0],
            lockData: lockData,
            dialogData: dialogData,
            shopData: null,
            encounterData: null,
            timestamp: null,
            globalStateIndex: globalStateIndex);

    [Fact]
    public void TheDoorVariantIsTheGlobalStateBlock() {
        // Bit 0x20 is SUBREC_DOOR_VARIANT, 2 bytes — our field is named GlobalStateIndex but it is
        // the door's identity, not a global-state key.
        Assert.Equal(37, FixedObjectAccess.DoorVariant(Object(globalStateIndex: 37)));
        Assert.Null(FixedObjectAccess.DoorVariant(Object()));
    }

    [Fact]
    public void TheLockValueIsByteOneOfTheParamsBlock() {
        // Doors read it as interact_msg.bFlags, ladders as door_or_npc_key.bLookup_key — the same
        // byte under two names, which our chest-flavoured model calls Difficulty.
        var locked = new SaveGameContainerLockData(flags: 0, difficulty: 60, puzzleChest: 0, trapDamage: 0);

        Assert.Equal(60, FixedObjectAccess.LockValue(Object(lockData: locked)));
        Assert.True(FixedObjectAccess.IsLocked(Object(lockData: locked)));
    }

    [Fact]
    public void NoParamsBlockMeansUnlocked() {
        Assert.Equal(0, FixedObjectAccess.LockValue(Object()));
        Assert.False(FixedObjectAccess.IsLocked(Object()));
    }

    [Fact]
    public void AZeroDifficultyIsAlsoUnlocked() {
        var open = new SaveGameContainerLockData(flags: 4, difficulty: 0, puzzleChest: 0, trapDamage: 0);

        Assert.False(FixedObjectAccess.IsLocked(Object(lockData: open)));
    }

    [Fact]
    public void TheInteractDialogIsWhereLadderTraversalLives() {
        var dialog = new SaveGameContainerDialogData(examineMessageIndex: 3, flags: 0, dialogId: 1_500_107);

        Assert.Equal(1_500_107L, FixedObjectAccess.InteractDialogId(Object(dialogData: dialog)));
        Assert.Equal(3, FixedObjectAccess.ExamineMessageIndex(Object(dialogData: dialog)));
    }

    [Fact]
    public void AnObjectWithNoBlocksAnswersNothingRatherThanThrowing() {
        Assert.Null(FixedObjectAccess.InteractDialogId(Object()));
        Assert.Null(FixedObjectAccess.ExamineMessageIndex(Object()));
        Assert.Null(FixedObjectAccess.DoorVariant(null));
        Assert.Equal(0, FixedObjectAccess.LockValue(null));
    }

    [Fact]
    public void ADoorsOpenFlagFollowsFromItsVariant() {
        // The two halves meet here: the variant identifies the door, and DoorMechanics turns it
        // into the global flag its open state is stored in.
        int? variant = FixedObjectAccess.DoorVariant(Object(globalStateIndex: 12));

        Assert.Equal(7012,
            GameData.Resources.World.DoorMechanics.OpenFlagBase + variant!.Value);
    }

    [Fact]
    public void ALockValueFeedsTheShardLockPickingTiers() {
        var locked = new SaveGameContainerLockData(flags: 0, difficulty: 81, puzzleChest: 0, trapDamage: 0);

        Assert.Equal(3, GameData.Resources.Character.LockPicking.DifficultyTier(
            FixedObjectAccess.LockValue(Object(lockData: locked))));
    }
}
