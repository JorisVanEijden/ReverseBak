namespace BetrayalAtKrondor.Tests.World;

using global::GameData.Resources.World;
using Xunit;

public class EntityFlagDecodeTests {
    [Theory]
    [InlineData((byte)0x00, false, false)]
    [InlineData((byte)0x20, true, false)]
    [InlineData((byte)0x40, false, true)]
    [InlineData((byte)0x60, true, true)]
    public void EntityFlags_decodes_to_named_booleans(byte flags, bool unbounded, bool depthSorted) {
        var dat = new TableDatInfo { EntityFlags = flags };
        Assert.Equal(unbounded, dat.IsUnbounded);
        Assert.Equal(depthSorted, dat.IsDepthSorted);
    }
}
