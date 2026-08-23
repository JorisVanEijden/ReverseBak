namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;

using Xunit;

/// <summary>
/// How big a world sprite is — <c>renderSprite2</c> @0x23031 with <c>RenderWorldItem</c> @0x2a95a.
/// </summary>
public class SpriteBillboardSizeTests {
    [Fact]
    public void SizeScaleIsAFractionInOneHundredTwentyEighths() {
        // 128 is "the entity's own extent"; 64 is half of it.
        Assert.Equal(1000, SpriteBMeshFace.WorldExtentFor(sizeScale: 128, entityExtent: 1000));
        Assert.Equal(500, SpriteBMeshFace.WorldExtentFor(sizeScale: 64, entityExtent: 1000));
    }

    [Fact]
    public void ZeroMeansTwiceTheExtent_NotZeroSize() {
        // The trap: read literally, SizeScale 0 makes the sprite vanish. The engine instead uses
        // the entity extent directly, which spans 2x — equivalent to 256.
        Assert.Equal(SpriteBMeshFace.SizeScaleWhenZero, 256);
        Assert.Equal(2000, SpriteBMeshFace.WorldExtentFor(sizeScale: 0, entityExtent: 1000));
        Assert.Equal(SpriteBMeshFace.WorldExtentFor(sizeScale: 256, entityExtent: 1000),
            SpriteBMeshFace.WorldExtentFor(sizeScale: 0, entityExtent: 1000));
    }

    [Fact]
    public void ANegativeExtentIsADirection_NotANegativeSize() {
        Assert.Equal(SpriteBMeshFace.WorldExtentFor(100, 1000),
            SpriteBMeshFace.WorldExtentFor(100, -1000));
    }

    [Fact]
    public void TheTexturesPixelSizeDoesNotAffectIt() {
        // Only the ASPECT comes from the bitmap; the extent is entity-derived. Expressed as the
        // absence of a texture parameter, and asserted here so the point survives a refactor that
        // is tempted to pass one in.
        Assert.Equal(320, SpriteBMeshFace.WorldExtentFor(sizeScale: 40, entityExtent: 1024));
    }
}
