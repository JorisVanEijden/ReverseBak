namespace BetrayalAtKrondor.Tests.World;

using global::GameData.Resources.World;
using Xunit;

/// <summary>
/// Pins the decode of the ZoneTable flag bytes on the model, so consumers read semantics instead
/// of re-masking raw bytes. Mirrors the TableDatInfo.IsUnbounded/IsDepthSorted pattern.
/// </summary>
public class ZoneTableFlagTests {
    [Theory]
    [InlineData(0x00, FaceCullMode.DoubleSided)]
    [InlineData(0x01, FaceCullMode.SingleSided)]
    [InlineData(0x02, FaceCullMode.Skip)]
    [InlineData(0x03, FaceCullMode.SingleSided)]  // 1 and 3 both mean single-sided cull
    public void Cull_mode_decodes_from_flag_bits_0_and_1(byte flags, FaceCullMode expected) {
        Assert.Equal(expected, new PolygonFace { Flags = flags }.CullMode);
    }

    [Theory]
    [InlineData(0x81, FaceCullMode.SingleSided)]  // house face: shading bits set, cull bits = 1
    [InlineData(0x91, FaceCullMode.SingleSided)]  // chest face: textured bit set too
    [InlineData(0xA0, FaceCullMode.DoubleSided)]  // high bits set, cull bits = 0
    public void Cull_mode_ignores_the_unrelated_high_bits(byte flags, FaceCullMode expected) {
        // Bits 4/5/7 carry texturing and shading; they must not leak into the cull decision.
        Assert.Equal(expected, new PolygonFace { Flags = flags }.CullMode);
    }

    [Theory]
    [InlineData(0x00, false)]
    [InlineData(0x02, true)]
    [InlineData(0x03, true)]   // bit 0 is unrelated
    [InlineData(0x01, false)]
    public void Gid_sloped_decodes_from_flag_bit_1(byte flags, bool expected) {
        // Selects the 10-byte sloped region stride over the 6-byte flat one; was masked
        // independently in the extractor and again in the Unity debug overlay.
        Assert.Equal(expected, new TableGidInfo { Flags = flags }.IsSloped);
    }
}
