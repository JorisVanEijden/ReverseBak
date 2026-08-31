namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// Firing the catapult — <c>handle_Catapult</c> (ovr190 @0x770aa).
/// </summary>
/// <remarks>
/// <b>TASK-136 says it "summons a scripted actor". It does not.</b> There is no actor, no encounter
/// spawn and no roster touch in the routine — it animates the world item's own sprite frame, plays a
/// sound and disposes the container.
/// </remarks>
public class CatapultUseTests {
    [Fact]
    public void TWOGatesThatReadAlikeAndAreNot() {
        // A key of ZERO means the object has no story attached — "this must not be very important".
        // A real key whose VALUE is zero means the story has not reached this point: the container's
        // own dialog shows and then it stops. Collapsing them loses that second line entirely.
        Assert.False(CatapultUse.HasAnything(globalKey: 0));
        Assert.True(CatapultUse.HasAnything(globalKey: 42));

        Assert.False(CatapultUse.Fires(globalKey: 0, globalValue: 1));
        Assert.False(CatapultUse.Fires(globalKey: 42, globalValue: 0));
        Assert.True(CatapultUse.Fires(globalKey: 42, globalValue: 1));
    }

    [Fact]
    public void ITISAFLAG_notAnItemAndNotASkill() {
        // Worth asserting against the grave, which gates on carrying a shovel. Two objects in the
        // same handler family, two completely different gates.
        Assert.True(CatapultUse.Fires(1, 1));
        Assert.NotEqual(GraveDigging.ShovelObjectId, 0);
    }

    [Fact]
    public void THEANIMATIONIsFOURFramesDownThenUp_notASingleSweep() {
        // Two loops: the first counts DOWN from 1 to 0, the second UP from 0 to 1, each frame
        // redrawn. A single 0..1 sweep drops half the motion.
        Assert.Equal(new[] { 1, 0, 0, 1 }, CatapultUse.FrameSequence);
        Assert.Equal(4, CatapultUse.FrameSequence.Length);
    }

    [Fact]
    public void ONLYTheLowByteOfTheFrameWordIsWritten() {
        // ax = (current & 0xFF00) | frame. The high byte carries something else and survives every
        // step; writing the whole word would clear it.
        Assert.Equal(0xAB01, CatapultUse.FrameWord(0xABCD, 1));
        Assert.Equal(0xAB00, CatapultUse.FrameWord(0xABCD, 0));
        Assert.Equal(0x0001, CatapultUse.FrameWord(0x00FF, 1));
    }

    [Fact]
    public void EveryFrameKeepsTheHighByteAcrossTheWholeSequence() {
        var word = 0x7F00;
        foreach (int frame in CatapultUse.FrameSequence) {
            word = CatapultUse.FrameWord(word, frame);
            Assert.Equal(0x7F00, word & 0xFF00);
        }
    }

    [Fact]
    public void THESOUNDFollowsTheAnimation() {
        // All four frames are drawn and presented, and only then does the sound play. On the first
        // frame it would put the crack of the arm before the arm moves. It is also loaded and
        // unloaded around the sequence rather than living in a resident bank.
        Assert.True(CatapultUse.SoundFollowsTheAnimation);
    }

    [Fact]
    public void APRIMARYClickDisposesTheContainerWhetherItFiredOrNot() {
        // The dispose is on the shared tail all three primary outcomes reach — no key, key unset,
        // and the full firing. Only the secondary-click examine returns without it, so the object is
        // one-shot from the first primary click whatever that click achieved.
        Assert.True(CatapultUse.PrimaryClickAlwaysDisposesTheContainer);
        Assert.Equal(167, CatapultUse.ExamineDialog);
    }

    [Fact]
    public void ITSHARESTheGravesGenericNothingHereLine() {
        Assert.Equal(GraveDigging.NothingHereDialog, CatapultUse.NothingHereDialog);
    }
}
