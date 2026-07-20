namespace BetrayalAtKrondor.Tests.World;

using System.IO;
using System.Text;
using global::GameData.Resources.World;
using global::ResourceExtraction.Extractors;
using Xunit;

/// <summary>
/// Structural tests over <see cref="ZoneTableExtractor"/> driven by a hand-built, minimal
/// TBL stream — no OriginalGame data required. The synthetic file exercises the full
/// MAP → DAT → entity → LOD → mesh → mesh-face → polygon-face walk with exactly one of each,
/// so a field read from the wrong stream position is directly observable.
/// </summary>
public class ZoneTableExtractorTests {
    static ZoneTableExtractorTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    // Distinctive sentinel for the mesh-face record's +0x06 field. Chosen so a read from any
    // other stream position cannot coincidentally produce it.
    private const ushort Unknown06Sentinel = 0xBEEF;

    [Fact]
    public void Polygon_mesh_face_reads_Unknown06_from_its_own_record() {
        var table = Extract(BuildMinimalTbl());

        var meshFace = Assert.IsType<PolygonMeshFace>(
            table.Entries[0].Dat.Lods[0].Meshes[0].MeshFaces[0]);

        // Regression: ReadPolygonFaces seeks the shared BinaryReader to the face array and
        // never restores position, so reading +0x06 after it returned yielded whatever byte
        // the face walk happened to stop on.
        Assert.Equal(Unknown06Sentinel, meshFace.Unknown06);
    }

    [Fact]
    public void Polygon_face_payload_survives_the_walk() {
        // Guards the sentinel test above: proves the face array really was parsed, so a
        // "fix" that simply skipped ReadPolygonFaces would not pass silently.
        var table = Extract(BuildMinimalTbl());
        var meshFace = Assert.IsType<PolygonMeshFace>(
            table.Entries[0].Dat.Lods[0].Meshes[0].MeshFaces[0]);

        var face = Assert.Single(meshFace.Faces);
        Assert.Equal(0x01, face.Flags);
        Assert.Equal(2, face.VgaColor);
        Assert.Equal(6, face.NormalVertexIndex);
    }

    // GID sloped-region sentinels. Chosen so the ×1.2 bake is observable and, for A, so the
    // result needs rounding (7 × 1.2 = 8.4 → 8) rather than landing exactly.
    private const short GidBaseElevation = 250;   // → 300
    private const sbyte GidSlopeA = 7;            // → 8
    private const sbyte GidSlopeB = -5;           // → -6
    private const byte GidSlopeMagnitude = 11;
    private const byte GidSlopeBearing = 0xA0;

    [Fact]
    public void Sloped_region_base_elevation_gets_the_world_up_aspect_bake() {
        var region = Assert.Single(Extract(BuildMinimalTbl(withGid: true)).Entries[0].Gid.Regions);

        // DAT vertices and bboxes are baked ×1.2 (WorldUpAspectScale). GID elevations describe
        // the same world-up axis, so an unscaled elevation sits 20% below its own geometry.
        Assert.Equal(300, region.BaseElevation);
    }

    [Fact]
    public void Sloped_region_gradient_scales_with_its_elevation() {
        var region = Assert.Single(Extract(BuildMinimalTbl(withGid: true)).Entries[0].Gid.Regions);

        // elevation(P) = Base + ((A·dx + B·dy) · SlopeShift) >> 12 — the gradient term is also a
        // Z value, so scaling Base alone would pivot the ramp instead of stretching it.
        Assert.NotNull(region.Slope);
        Assert.Equal(8, region.Slope!.A);    // 7 × 1.2 = 8.4 → 8
        Assert.Equal(-6, region.Slope!.B);   // -5 × 1.2 = -6
    }

    [Fact]
    public void Editor_computed_slope_summaries_are_preserved_verbatim() {
        var region = Assert.Single(Extract(BuildMinimalTbl(withGid: true)).Entries[0].Gid.Regions);

        // SlopeMagnitude is an editor-time steepness summary and SlopeBearing is a horizontal
        // compass angle — neither is a Z value, so the aspect bake must not touch them.
        Assert.Equal(GidSlopeMagnitude, region.SlopeMagnitude);
        Assert.Equal(GidSlopeBearing, region.SlopeBearing);
    }

    // Geometry fixture: VertexScale 2 → factor 4. Raw values chosen so the baked results are
    // unambiguous and the Z bake (×1.2, applied after the shift) lands exactly.
    private const byte GeomVertexScale = 2;
    private const short GeomVertexX = 100;   // → 400
    private const short GeomVertexY = 200;   // → 800
    private const short GeomVertexZ = 300;   // → 300 × 4 × 1.2 = 1440
    private const short GeomExtent = 1000;   // → 4000
    private const short GeomBboxMinZ = -50;  // → -50 × 4 × 1.2 = -240

    [Fact]
    public void Vertex_pool_is_stored_pre_scaled_by_the_vertex_scale_exponent() {
        var dat = Extract(BuildTblWithGeometry()).Entries[0].Dat;

        // VertexScale is a DOS storage-compression exponent. Baking it here means no consumer
        // has to know it exists — and none can forget to apply it.
        var v = Assert.Single(dat.Lods[0].VertexPools[0]);
        Assert.Equal(GeomVertexX << GeomVertexScale, v.X);
        Assert.Equal(GeomVertexY << GeomVertexScale, v.Y);
        Assert.Equal(1440, v.Z);   // shift first, then the ×1.2 world-up bake
    }

    [Fact]
    public void Bounding_box_is_scaled_the_same_way_as_the_vertices_it_bounds() {
        var dat = Extract(BuildTblWithGeometry()).Entries[0].Dat;

        Assert.Equal(-240, dat.Min!.Z);
    }

    [Fact]
    public void Extent_is_stored_pre_scaled() {
        var dat = Extract(BuildTblWithGeometry()).Entries[0].Dat;

        // The engine's dword_3803 = Extent << VertexScale; ship that value, not the operands.
        Assert.Equal(GeomExtent << GeomVertexScale, dat.Extent);
    }

    /// <summary>
    /// One bounded entity carrying a bbox and a single-vertex pool, so the vertex-scale bake is
    /// observable on vertices, bbox and extent. DAT-relative layout (segmentBase = 0x10):
    ///   0x10 entity header 14 bytes (EF_UNBOUNDED clear → bbox follows)
    ///   0x1E bbox          12 bytes
    ///   0x30 LOD record     6 bytes
    ///   0x40 mesh record   14 bytes
    ///   0x50 vertex array   6 bytes
    /// </summary>
    private static byte[] BuildTblWithGeometry() {
        var map = new MemoryStream();
        var m = new BinaryWriter(map);
        m.Write((ushort)1); m.Write((ushort)1); m.Write((ushort)0); m.Write((ushort)5);
        m.Write(Encoding.ASCII.GetBytes("test\0"));

        var dat = new MemoryStream();
        var d = new BinaryWriter(dat);
        d.Write((ushort)0);                  // pointer lower
        d.Write((ushort)1);                  // pointer upper → segmentBase 0x10
        Pad(d, 0x10);

        d.Write((byte)0x00);                 // EntityFlags: bounded → bbox present
        d.Write((byte)0);                    // EntityType
        d.Write((byte)0);                    // DrawPriority
        d.Write(GeomVertexScale);            // VertexScale
        d.Write((ushort)0);                  // Unknown04
        d.Write((ushort)0);                  // Unknown06
        d.Write((ushort)1);                  // LodCount
        d.Write((ushort)0x20);               // lodArrayOffset (near) → 0x30
        d.Write(GeomExtent);
        // bbox at +0x0E
        d.Write((short)0); d.Write((short)0); d.Write(GeomBboxMinZ);   // Min
        d.Write((short)0); d.Write((short)0); d.Write((short)0);       // Max
        Pad(d, 0x30);

        d.Write((ushort)0);                  // LOD Threshold
        d.Write((ushort)1);                  // LOD MeshCount
        d.Write((ushort)0x30);               // meshBaseOffset (near) → 0x40
        Pad(d, 0x40);

        d.Write((byte)0); d.Write((byte)0); d.Write((byte)0);
        d.Write((byte)1);                    // VertexCount = 1
        d.Write((ushort)0x40);               // pVertexArray (near) → 0x50
        d.Write((ushort)0);                  // MeshFaceCount
        d.Write((ushort)0);                  // pMeshFaceData
        d.Write((ushort)0);                  // ChildCount
        d.Write((ushort)0);                  // pChildren
        Pad(d, 0x50);

        d.Write(GeomVertexX); d.Write(GeomVertexY); d.Write(GeomVertexZ);

        var file = new MemoryStream();
        var f = new BinaryWriter(file);
        WriteSection(f, "MAP:", map.ToArray());
        WriteSection(f, "DAT:", dat.ToArray());
        return file.ToArray();
    }

    private static ZoneTable Extract(byte[] tbl) {
        using var stream = new MemoryStream(tbl);
        return new ZoneTableExtractor().Extract("Z99.TBL", stream);
    }

    /// <summary>
    /// One entity, one sloped region. GID-relative layout (segmentBase = 0x10):
    ///   0x00 pointer table   lower=0, upper=1 → entity at +0x10
    ///   0x10 gid header      8 bytes, Flags 0x02 = sloped → region stride 10
    ///   0x20 region          10 bytes
    ///   0x30 slope plane     6 bytes
    /// </summary>
    private static byte[] BuildGidSection() {
        var gid = new MemoryStream();
        var g = new BinaryWriter(gid);
        g.Write((ushort)0);                 // pointer lower
        g.Write((ushort)1);                 // pointer upper → segmentBase 0x10
        Pad(g, 0x10);

        g.Write((short)0);                  // XRadius
        g.Write((short)0);                  // YRadius
        g.Write((byte)0x02);                // Flags: sloped
        g.Write((byte)1);                   // regionCount
        g.Write((ushort)0x10);              // pRegions (near) → 0x20
        Pad(g, 0x20);

        g.Write((ushort)0);                 // pSubedges (0 → no subedge walk)
        g.Write((byte)0);                   // subedgeCount
        g.Write((byte)3);                   // SlopeShift
        g.Write(GidBaseElevation);          // +0x04
        g.Write((ushort)0x20);              // pSlopePlane (near) → 0x30
        g.Write(GidSlopeMagnitude);         // +0x08
        g.Write(GidSlopeBearing);           // +0x09
        Pad(g, 0x30);

        g.Write(GidSlopeA);
        g.Write(GidSlopeB);
        g.Write((short)0);                  // AnchorX
        g.Write((short)0);                  // AnchorY
        return gid.ToArray();
    }

    /// <summary>
    /// One entity, one LOD, one mesh, one polygon mesh-face, one polygon face.
    /// DAT-relative layout (segmentBase = 16, so near-pointers are absolute minus 16):
    ///   0x00 pointer table   lower=0, upper=1  → entity at +0x10, segmentBase 0x10
    ///   0x10 entity header   14 bytes, EF_UNBOUNDED set so no bbox follows
    ///   0x20 LOD record      6 bytes
    ///   0x30 mesh record     14 bytes
    ///   0x40 mesh-face rec   8 bytes  ← carries Unknown06Sentinel at +0x06
    ///   0x50 polygon face    8 bytes
    ///   0x58 tail padding    2 bytes  (so a stray read lands on 0x0000, not EOF)
    /// </summary>
    private static byte[] BuildMinimalTbl(bool withGid = false) {
        var map = new MemoryStream();
        var m = new BinaryWriter(map);
        m.Write((ushort)1);          // Capacity
        m.Write((ushort)1);          // numItems
        m.Write((ushort)0);          // offsets[0]
        m.Write((ushort)5);          // StringPoolSize
        m.Write(Encoding.ASCII.GetBytes("test\0"));

        var dat = new MemoryStream();
        var d = new BinaryWriter(dat);
        d.Write((ushort)0);          // pointer lower → (lower & 0xF) = 0
        d.Write((ushort)1);          // pointer upper → segmentBase = 1 << 4 = 0x10
        Pad(d, 0x10);

        d.Write((byte)0x20);         // EntityFlags: EF_UNBOUNDED (no bbox)
        d.Write((byte)0);            // EntityType
        d.Write((byte)0);            // DrawPriority
        d.Write((byte)0);            // VertexScale
        d.Write((ushort)0);          // Unknown04
        d.Write((ushort)0);          // Unknown06 (entity-level)
        d.Write((ushort)1);          // LodCount
        d.Write((ushort)0x10);       // lodArrayOffset (near) → 0x10 + 0x10 = 0x20
        d.Write((short)0);           // Extent
        Pad(d, 0x20);

        d.Write((ushort)0);          // LOD Threshold
        d.Write((ushort)1);          // LOD MeshCount
        d.Write((ushort)0x20);       // meshBaseOffset (near) → 0x30
        Pad(d, 0x30);

        d.Write((byte)0);            // RuntimeFlagsIndex
        d.Write((byte)0);            // SortNormalVertex
        d.Write((byte)0);            // SortAnchorVertex
        d.Write((byte)0);            // VertexCount (0 → no vertex pool)
        d.Write((ushort)0);          // pVertexArray
        d.Write((ushort)1);          // MeshFaceCount
        d.Write((ushort)0x30);       // pMeshFaceData (near) → 0x40
        d.Write((ushort)0);          // ChildCount
        d.Write((ushort)0);          // pChildren
        Pad(d, 0x40);

        d.Write((byte)0);            // renderType 0 = polygon
        d.Write((byte)0);            // padding01
        d.Write((ushort)1);          // FaceCount
        d.Write((ushort)0x40);       // pFaceArray (near) → 0x50
        d.Write(Unknown06Sentinel);  // +0x06
        Pad(d, 0x50);

        d.Write((byte)0x01);         // Flags
        d.Write((byte)2);            // VgaColor
        d.Write((byte)3);            // VgaShade
        d.Write((byte)4);            // EgaColor
        d.Write((byte)5);            // EgaShade
        d.Write((byte)6);            // NormalVertexIndex
        d.Write((ushort)0);          // pVertexIndexList (0 → no index list walk)
        d.Write((ushort)0);          // tail padding

        var file = new MemoryStream();
        var f = new BinaryWriter(file);
        WriteSection(f, "MAP:", map.ToArray());
        if (withGid) WriteSection(f, "GID:", BuildGidSection());
        WriteSection(f, "DAT:", dat.ToArray());
        return file.ToArray();
    }

    private static void Pad(BinaryWriter w, long to) {
        while (w.BaseStream.Position < to) w.Write((byte)0);
    }

    private static void WriteSection(BinaryWriter w, string tag, byte[] payload) {
        w.Write(Encoding.ASCII.GetBytes(tag));
        w.Write((uint)payload.Length);
        w.Write(payload);
    }
}
