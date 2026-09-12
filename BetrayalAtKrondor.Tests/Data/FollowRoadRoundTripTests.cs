namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using ResourceExtraction;
using System;
using System.Text;
using Xunit;

/// <summary>
/// Follow-road (<c>bIsAutoTravelling</c>, body offset 50) survives a write.
/// </summary>
/// <remarks>
/// It was parsed and never written, which is how a port save handed the original a party free to
/// leave the road when the player had travel engaged. The four shipped chapter-1 saves this project
/// drives from carry a 1 there, and with it set the original refuses every off-road step — see
/// TASK-422.
/// </remarks>
public class FollowRoadRoundTripTests {
    static FollowRoadRoundTripTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private const int Offset = SaveGameOffsets.IsAutoTravelling;

    [Theory]
    [InlineData((short)1)]
    [InlineData((short)0)]
    public void TheFlagIsWrittenWhereTheOriginalReadsIt(short engaged) {
        byte[] body = Body();
        BitConverter.GetBytes((short)(engaged == 0 ? 1 : 0)).CopyTo(body, Offset);

        SaveGameWriteResult r = SaveGameWriter.Write(body, Fields(engaged), "s", 0, 0, 0);

        Assert.Equal(engaged, BitConverter.ToInt16(BodyOf(r), Offset));
    }

    [Fact]
    public void NotSupplyingItLeavesTheSavesOwnAlone() {
        // The nullable contract MapCameraZ established: the writer clones the body and overwrites
        // only what it is handed, so a caller that knows nothing about travel must not clear it.
        byte[] body = Body();
        BitConverter.GetBytes((short)1).CopyTo(body, Offset);

        SaveGameWriteResult r = SaveGameWriter.Write(body, Fields(null), "s", 0, 0, 0);

        Assert.Equal(1, BitConverter.ToInt16(BodyOf(r), Offset));
    }

    private static byte[] Body() => new byte[SaveGameOffsets.BodySize];

    private static SaveGameFields Fields(short? travelling) => new SaveGameFields(
        Chapter: 1, PartyGold: 0, GameTime: 0, TimeSnapshot: 0, PaletteEventMask: 0,
        PartyDeathState: 0, ChapterTransitionPending: 0, PreviousZone: 1, CurrentZone: 1,
        WorldX: 0, WorldY: 0, PositionX: 0, PositionY: 0, PositionZ: 0, Rotation: 0,
        IsAutoTravelling: travelling);

    private static byte[] BodyOf(SaveGameWriteResult r) =>
        r.Bytes[SaveGameOffsets.HeaderSize..];
}
