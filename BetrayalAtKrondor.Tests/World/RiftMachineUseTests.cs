namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// Switching on the rift machine — <c>handle_RiftMachine</c> (ovr190 @0x782c0).
/// </summary>
/// <remarks>
/// <b>TASK-136 calls it "another shovel/dig object". It is not one.</b> No shovel, no digging, no
/// item test — it is gated on a global flag and plays an effect, like the catapult but by a
/// different mechanism.
/// </remarks>
public class RiftMachineUseTests {
    [Fact]
    public void ITACCEPTSTWOContainerTypes_whereItsSiblingsAcceptOne() {
        // *** The trap for a shared helper. *** The grave and the catapult both require
        // fixedWorldItem alone; this one also takes type 9. An "is this a world container" helper
        // written from either sibling refuses half the rift machines, silently.
        Assert.True(RiftMachineUse.AcceptsContainerType(RiftMachineUse.FixedWorldItemType));
        Assert.True(RiftMachineUse.AcceptsContainerType(RiftMachineUse.SecondAcceptedType));
        Assert.False(RiftMachineUse.AcceptsContainerType(0));
        Assert.NotEqual(RiftMachineUse.FixedWorldItemType, RiftMachineUse.SecondAcceptedType);
    }

    [Fact]
    public void SameTwoGateShapeAsTheCatapult() {
        Assert.False(RiftMachineUse.Runs(globalKey: 0, globalValue: 1));
        Assert.False(RiftMachineUse.Runs(globalKey: 7, globalValue: 0));
        Assert.True(RiftMachineUse.Runs(globalKey: 7, globalValue: 1));
    }

    [Fact]
    public void THEEFFECTIsGLOBAL_whereTheCatapultsIsPerObject() {
        // The rift machine raises a global render flag for ten frames; the catapult steps its own
        // item's sprite byte and touches nothing global. Two objects, two mechanisms — a shared
        // "play the effect" helper fits neither.
        Assert.Equal(10, RiftMachineUse.EffectFrames);
        Assert.Equal(4, CatapultUse.FrameSequence.Length);
        Assert.NotEqual(RiftMachineUse.EffectFrames, CatapultUse.FrameSequence.Length);
    }

    [Fact]
    public void ITSettlesAfterwards_orTheLastFrameStillCarriesTheEffect() {
        // The flag is cleared and the world redrawn twice more. Without those the final frame the
        // player sees is the one with the effect still on it.
        Assert.Equal(2, RiftMachineUse.SettleFrames);
        Assert.True(RiftMachineUse.SettleFrames > 0);
    }

    [Fact]
    public void ITUnloadsOnlyTheSoundItActuallyStarted() {
        // audio_sound_play's return is kept and tested before the unload. Unloading unconditionally
        // releases a bank the play never took.
        Assert.True(RiftMachineUse.UnloadsOnlyWhatItLoaded);
    }

    [Fact]
    public void APRIMARYClickDisposesTheContainerWhetherItRanOrNot() {
        Assert.True(RiftMachineUse.PrimaryClickAlwaysDisposesTheContainer);
        Assert.True(CatapultUse.PrimaryClickAlwaysDisposesTheContainer);
    }

    [Fact]
    public void ALLTHREEHandlersShareTheGenericNothingHereLine() {
        Assert.Equal(GraveDigging.NothingHereDialog, RiftMachineUse.NothingHereDialog);
        Assert.Equal(CatapultUse.NothingHereDialog, RiftMachineUse.NothingHereDialog);
        // But each has its OWN examine line — the secondary click is object-specific.
        Assert.NotEqual(GraveDigging.ExamineDialog, RiftMachineUse.ExamineDialog);
        Assert.NotEqual(CatapultUse.ExamineDialog, RiftMachineUse.ExamineDialog);
    }
}
