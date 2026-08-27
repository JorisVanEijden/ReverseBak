namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// Where the camera looks while a backdrop dialog is up (TASK-223).
/// </summary>
/// <remarks>
/// The numbers are read from <c>ExecuteDialog</c>'s backdrop arm (IDA 0x49a10-0x49a98), not from
/// canassa: an XOR that a port naturally writes as an addition, and two slot offsets that are easy
/// to apply to the wrong member.
/// </remarks>
public class DialogBackdropCameraTests {
    private static readonly byte[] Marching = { 0, 2, 1 };   // Locklear, Owyn, Gorath

    [Fact]
    public void OnlyAWALKINGPartyMemberTurnsTheCamera() {
        // Actor numbers are one-based, so 1 is character 0.
        Assert.Equal(0, DialogBackdropCamera.SpeakerSlot(Marching, 1));
        Assert.Equal(1, DialogBackdropCamera.SpeakerSlot(Marching, 3));
        Assert.Equal(2, DialogBackdropCamera.SpeakerSlot(Marching, 2));
    }

    [Fact]
    public void ACHARACTERNOTINTHEPARTYIsMinusOneRatherThanSlotZero() {
        // *** The trap. *** The original's loop leaves its counter at 0 when nothing matches, so a
        // port that returns the counter frames a stranger as though they led the party. Pug, James
        // and Patrus are in the cast and not in this marching order.
        Assert.Equal(-1, DialogBackdropCamera.SpeakerSlot(Marching, 4));
        Assert.False(DialogBackdropCamera.TurnsCamera(Marching, 4));
    }

    [Fact]
    public void ASPEAKERWHOISNOTAPARTYMEMBERLeavesTheCameraAlone() {
        // A townsman, a sign, a narrator: above the six, so the gate rejects them before the
        // active-party test is even reached.
        Assert.Equal(-1, DialogBackdropCamera.SpeakerSlot(Marching, 7));
        Assert.Equal(-1, DialogBackdropCamera.SpeakerSlot(Marching, 0));
        Assert.Equal(0x1234, DialogBackdropCamera.YawFor(0x1234, -1));
    }

    [Fact]
    public void TheLeaderIsTurnedABOUTFACEAndNothingMore() {
        Assert.Equal((ushort)(0x1234 ^ 0x8400), DialogBackdropCamera.YawFor(0x1234, 0));
    }

    [Fact]
    public void ITISANXORNOTANADDITION() {
        // *** The whole reason this constant is worth a test. *** For a yaw whose 0x0400 bit is
        // SET, xor and add give different answers, and both look plausible in isolation.
        const ushort yaw = 0x0400;
        Assert.Equal((ushort)0x8000, DialogBackdropCamera.YawFor(yaw, 0));
        Assert.NotEqual((ushort)(yaw + 0x8400), DialogBackdropCamera.YawFor(yaw, 0));
    }

    [Fact]
    public void TheSecondAndThirdCompanionsAreTurnedOppositeWays() {
        ushort leader = DialogBackdropCamera.YawFor(0x1234, 0);
        Assert.Equal((ushort)(leader - 0x2800), DialogBackdropCamera.YawFor(0x1234, 1));
        Assert.Equal((ushort)(leader + 0x2800), DialogBackdropCamera.YawFor(0x1234, 2));
    }

    [Fact]
    public void TheOffsetsWrapIn16Bits() {
        // Widen the arithmetic and the second companion's bearing lands somewhere the game never
        // looks. 0x8400 ^ 0x8400 == 0, and 0 - 0x2800 must come back as 0xd800.
        Assert.Equal((ushort)0xd800, DialogBackdropCamera.YawFor(0x8400, 1));
        Assert.Equal((ushort)0x2800, DialogBackdropCamera.YawFor(0x8400, 2));
    }

    [Fact]
    public void AFourthActiveMemberWouldTakeNoOffset() {
        // Only slots 1 and 2 are named in the original; anything else falls through the switch to
        // the plain about-face. The party never walks four, but the rule must not invent a number.
        byte[] four = { 0, 1, 2, 3 };
        Assert.Equal(3, DialogBackdropCamera.SpeakerSlot(four, 4));
        Assert.Equal(DialogBackdropCamera.YawFor(0x1234, 0), DialogBackdropCamera.YawFor(0x1234, 3));
    }
}
