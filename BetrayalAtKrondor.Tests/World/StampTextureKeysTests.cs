namespace BetrayalAtKrondor.Tests.World;

using global::GameData.Resources.World;
using global::ResourceExtraction.Extractors;
using Xunit;

public class StampTextureKeysTests {
    // Z01 slot image counts (bases 0,6,22,26,39).
    private static readonly int[] Z01Counts = { 6, 16, 4, 13, 14 };

    private static ZoneTable TableWith(params MeshFaceRecord[] faces) {
        var mesh = new MeshRecord();
        foreach (var f in faces) mesh.MeshFaces.Add(f);
        var lod = new LodLevel();
        lod.Meshes.Add(mesh);
        var dat = new TableDatInfo();
        dat.Lods.Add(lod);
        return new ZoneTable("Z01") { Entries = { new ZoneTableEntry { Dat = dat } } };
    }

    [Fact]
    public void Bakes_textured_quad_sprite_and_leaves_flat_null() {
        // chest face: Flags 0x91 (0x10 set), VgaColor 34, quad → SLOT3 img8
        var quad = new PolygonMeshFace { Faces = { new PolygonFace {
            Flags = 0x91, VgaColor = 34, VertexIndices = { 0, 1, 2, 3 } } } };
        // flat/house face: Flags 0x81 (0x10 clear) → null
        var flat = new PolygonMeshFace { Faces = { new PolygonFace {
            Flags = 0x81, VgaColor = 160, VertexIndices = { 0, 1, 2, 3 } } } };
        // sprite: BitmapIndex 22 → SLOT2 img0 (no Flags/quad gate)
        var sprite = new SpriteBMeshFace { BitmapIndex = 22 };

        var table = TableWith(quad, flat, sprite);
        ZoneTableExtractor.StampTextureKeys(table, 1, Z01Counts);

        Assert.Equal("Z01SLOT3.BMX#8", quad.Faces[0].TextureBitmap);
        Assert.Null(flat.Faces[0].TextureBitmap);
        Assert.Equal("Z01SLOT2.BMX#0", sprite.TextureBitmap);
    }

    [Fact]
    public void Non_quad_textured_face_and_out_of_range_stay_null() {
        var triTextured = new PolygonMeshFace { Faces = { new PolygonFace {
            Flags = 0x91, VgaColor = 34, VertexIndices = { 0, 1, 2 } } } };   // 0x10 but 3 verts
        var spriteOob = new SpriteBMeshFace { BitmapIndex = 200 };            // past total (53)

        var table = TableWith(triTextured, spriteOob);
        ZoneTableExtractor.StampTextureKeys(table, 1, Z01Counts);

        Assert.Null(triTextured.Faces[0].TextureBitmap);
        Assert.Null(spriteOob.TextureBitmap);
    }

    [Fact]
    public void Recurses_into_child_meshes() {
        var child = new MeshRecord();
        child.MeshFaces.Add(new SpriteBMeshFace { BitmapIndex = 0 });         // → SLOT0 img0
        var parent = new MeshRecord();
        parent.Children.Add(child);
        var lod = new LodLevel(); lod.Meshes.Add(parent);
        var dat = new TableDatInfo(); dat.Lods.Add(lod);
        var table = new ZoneTable("Z01") { Entries = { new ZoneTableEntry { Dat = dat } } };

        ZoneTableExtractor.StampTextureKeys(table, 1, Z01Counts);

        Assert.Equal("Z01SLOT0.BMX#0", ((SpriteBMeshFace)child.MeshFaces[0]).TextureBitmap);
    }
}
