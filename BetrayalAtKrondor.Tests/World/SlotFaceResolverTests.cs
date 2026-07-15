namespace BetrayalAtKrondor.Tests.World;

using global::GameData.Resources.World;
using global::ResourceExtraction.World;
using Xunit;

public class SlotFaceResolverTests {
    private static ZoneSlotBitmapIndex Z01() => new(new[] { 6, 16, 4, 13, 14 });

    [Fact]
    public void Textured_quad_resolves_to_slot_bitmap() {
        // chest face: Flags 0x91 (0x10 set), VgaColor 34, quad(4 verts) → SLOT3 img8
        var r = SlotFaceResolver.Resolve(0x91, 34, 4, Z01());
        Assert.Equal(new SlotBitmapRef(3, 8), r);
    }

    [Fact]
    public void Flat_face_returns_null() {
        // house face: Flags 0x81 (0x10 clear)
        Assert.Null(SlotFaceResolver.Resolve(0x81, 160, 4, Z01()));
    }

    [Fact]
    public void Non_quad_textured_face_returns_null() {
        // 0x10 set but 3 verts — original only textures quads
        Assert.Null(SlotFaceResolver.Resolve(0x91, 34, 3, Z01()));
    }

    [Fact]
    public void Out_of_range_index_returns_null() {
        Assert.Null(SlotFaceResolver.Resolve(0x91, 200, 4, Z01()));
    }
}
