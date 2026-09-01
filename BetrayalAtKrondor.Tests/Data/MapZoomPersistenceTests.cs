namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using ResourceExtraction;
using System.Text;
using Xunit;

/// <summary>
/// The overhead map's camera height survives a save — body offset 55, canassa
/// <c>lInsetCameraPosZ</c>.
/// </summary>
/// <remarks>
/// <b>It was READ and never WRITTEN.</b> The extractor has always parsed it (as
/// <c>SaveGameMovementData.SavedCameraZPosition</c>, renamed to <c>MapCameraZ</c> with this change)
/// and nothing put it back, so the player's map zoom reset to the zone default on every load.
///
/// <para>The half-ported shape is why it went unnoticed: a field that round-trips through the READER
/// looks modelled, and only the writer's offset list says whether it comes back.</para>
/// </remarks>
public class MapZoomPersistenceTests {
    // The writer stamps the save's name in codepage 437; without this every Write throws.
    static MapZoomPersistenceTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static byte[] Body() => new byte[SaveGameOffsets.BodySize];

    [Fact]
    public void TheZoomIsWrittenAtOffset55() {
        SaveGameWriteResult r = SaveGameWriter.Write(
            Body(), new SaveGameFields(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                MapCameraZ: 0x1234_5678),
            "test", 0, 0, 0);

        int at = r.Bytes.Length - SaveGameOffsets.BodySize;
        Assert.Equal(0x1234_5678, System.BitConverter.ToInt32(
            r.Bytes, at + SaveGameOffsets.MapCameraZ));
    }

    /// <summary>
    /// <b>Offset 55 is not offset 29.</b>
    /// </summary>
    /// <remarks>
    /// TASK-5 called this field "the camera Z we already author" and meant <c>PositionZ</c>, which
    /// is a different field twenty-six bytes earlier. Writing one must not disturb the other, and
    /// asserting only the new offset would pass just as well if they had been collapsed.
    /// </remarks>
    [Fact]
    public void ItDoesNotDisturbPositionZ() {
        SaveGameWriteResult r = SaveGameWriter.Write(
            Body(), new SaveGameFields(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, PositionZ: 4321,
                Rotation: 0, MapCameraZ: 8765),
            "test", 0, 0, 0);

        int at = r.Bytes.Length - SaveGameOffsets.BodySize;
        Assert.Equal(4321, System.BitConverter.ToInt32(r.Bytes, at + SaveGameOffsets.PositionZ));
        Assert.Equal(8765, System.BitConverter.ToInt32(r.Bytes, at + SaveGameOffsets.MapCameraZ));
        Assert.NotEqual(SaveGameOffsets.PositionZ, SaveGameOffsets.MapCameraZ);
    }

    /// <summary>The reader lands on the same offset the writer patches.</summary>
    /// <remarks>
    /// The extractor reads section 0 sequentially, so its position at this field is the sum of
    /// everything before it rather than a stated constant. That is exactly the arrangement where a
    /// writer offset and a reader stride can drift apart without either looking wrong, so the two
    /// are asserted against each other.
    /// </remarks>
    [Fact]
    public void TheReadersSequentialPositionAgreesWithTheWritersOffset() {
        // lastSeenStepSpeed(46,2) lastSeenGridStride(48,2) isAutoTraveling(50,2)
        // subTileStepCount(52,1) tileBoundaryCrossed(53,2) -> mapCameraZ at 55.
        Assert.Equal(SaveGameOffsets.LastSeenStepSpeed + 2, SaveGameOffsets.LastSeenGridStride);
        Assert.Equal(SaveGameOffsets.LastSeenGridStride + 2 + 2 + 1 + 2, SaveGameOffsets.MapCameraZ);
    }
}
