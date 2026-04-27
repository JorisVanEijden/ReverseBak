namespace GameData.Resources.World;

#if JSON_SERIALIZE
using System.Text.Json.Serialization;
#endif

/// <summary>
/// Parsed Z##.TBL file — maps world item TypeIds to visual assets and geometry.
/// Contains 4 tagged sections: MAP (names), APP (skipped), GID (grid/collision), DAT (rendering data).
/// </summary>
public class ZoneTable : IResource
{
    public ZoneTable(string id) { Id = id; }
    public ResourceType Type => ResourceType.TBL;
    public string Id { get; }
    public List<ZoneTableEntry> Entries { get; set; } = new();
    public MapSection MapSection { get; set; }
}

/// <summary>
/// Combined entry from MAP + GID + DAT sections, indexed by TypeId.
/// </summary>
public class ZoneTableEntry
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public TableDatInfo Dat { get; set; } = new();
    public TableGidInfo Gid { get; set; } = new();
}


public record MapSection
{
    public List<string> Names { get; set; }
    public ushort Unknown0 { get; set; }
    public ushort UnknownTail { get; set; }
}

/// <summary>
/// DAT-section top-level entity record — 14-byte header pointed to by the DAT pointer table,
/// consumed by RenderWorldItem (0x2a89f). NOT to be confused with <see cref="MeshRecord"/>:
/// both are 14 bytes and share offsets +0..+3 in byte layout but have different semantics
/// at +4..+0xD. See <c>docs/FileFormats/ZoneTable-DAT.md §1a</c>.
///
/// On-disk pointer encoding: each entry in the DAT pointer table is a 4-byte (lower, upper)
/// pair; the entity's flat byte offset in DAT is <c>(upper &lt;&lt; 4) + (lower &amp; 0xF)</c>,
/// and the entity's segment base for resolving internal near-pointers is <c>(upper &lt;&lt; 4)</c>.
/// All internal pointers (LodArrayOffset, MeshBaseOffset, PVertexArray, PMeshFaceData,
/// PChildren, PVertexIndexList) are interpreted as offsets relative to that segment base.
/// </summary>
public class TableDatInfo
{
    /// <summary>+0x00. Entity flags bitfield. Verified bits:
    ///   0x20 EF_UNBOUNDED — when SET, the 12-byte bbox is OMITTED (header is 14 bytes,
    ///        LOD records follow immediately). When CLEAR, the 12-byte bbox is present at
    ///        +0x0E..+0x18 and computeEntityViewExtent (0x238d7) uses it for view-extent
    ///        culling. Read at IDA 0x238e3 (`and al, 20h`).
    ///   0x40 EF_DEPTH_SORTED — when SET, cullEntityForRender (0x2a3bb) takes the
    ///        depth-sort path: per mesh it computes `dot(rotation1, rotation2)` where
    ///        rotation2 = pVertexArray[<see cref="MeshRecord.SortNormalVertex"/>] and
    ///        rotation1 = (pVertexArray[<see cref="MeshRecord.SortAnchorVertex"/>] &gt;&gt; shift) + global_anchor
    ///        (or just global_anchor when SortAnchorVertex == 0xFF), then SORTS the mesh
    ///        list by dot product descending (painter's algorithm). When CLEAR, the
    ///        simpler cull-only path is taken and the +1/+2 bytes are unread (always
    ///        0xFF in shipped data on non-flagged entities). Read at IDA 0x2a425.
    /// Other bits (0x01, 0x02, 0x04, 0x08, 0x10, 0x80) — no readers found; never set in
    /// shipped data (only observed values: 0x00, 0x40, 0x60).</summary>
    public byte EntityFlags { get; set; }

    /// <summary>+0x01. Entity-type tag.</summary>
    public byte EntityType { get; set; }

    /// <summary>+0x02. Terrain-type tag.</summary>
    public byte TerrainType { get; set; }

    /// <summary>+0x03. Shift count for vertex coords (RenderWorldItem 0x2a939 → g_vertexScale).
    /// World-space size = Extent &lt;&lt; VertexScale.</summary>
    public byte VertexScale { get; set; }

    /// <summary>+0x04. Purpose TBD — not read by any rendering-pipeline function. Non-zero
    /// only on ~20 interactive entity types (boom/gate/m_door/catapult); likely an
    /// interaction parameter consumed elsewhere. Observed: 0/1/2.</summary>
    public ushort Unknown04 { get; set; }

    /// <summary>+0x06. Purpose TBD — not read by any rendering-pipeline function. Non-zero
    /// only on the same ~20 interactive entities as Unknown04; small per-zone-varying
    /// integers (likely sound/event id or interaction target tag).</summary>
    public ushort Unknown06 { get; set; }

    /// <summary>+0x08. Number of LOD records at LodArrayOffset.</summary>
    public ushort LodCount { get; set; }

    /// <summary>+0x0C. Signed entity extent (RenderWorldItem 0x2a948: <c>movsx eax</c>),
    /// then shifted left by VertexScale → world-space extent for distance culling.</summary>
    public short Extent { get; set; }

    /// <summary>+0x0E..+0x13 (only when (EntityFlags &amp; 0x20) == 0 — bbox present).
    /// 3 i16: minX, minY, minZ. Read by computeEntityViewExtent (0x238d7).</summary>
#if JSON_SERIALIZE
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
    public Position3DShort? Min { get; set; }

    /// <summary>+0x14..+0x19 (only when (EntityFlags &amp; 0x20) == 0 — bbox present).
    /// 3 i16: maxX, maxY, maxZ. Read by computeEntityViewExtent (0x238d7).</summary>
#if JSON_SERIALIZE
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
    public Position3DShort? Max { get; set; }

    /// <summary>Resolved LOD records (length = LodCount).</summary>
    public List<LodLevel> Lods { get; set; } = new();
}

/// <summary>
/// LOD record — 6 bytes, walked by RenderWorldItem (0x2aa9d-0x2aab9): selects the entry
/// whose <see cref="Threshold"/> meets the runtime distance criterion. Each LOD then
/// references a contiguous array of 14-byte mesh records.
/// </summary>
public class LodLevel
{
    /// <summary>+0x00. Distance threshold (compared against <c>word_seg024_380F</c>).</summary>
    public ushort Threshold { get; set; }

    /// <summary>+0x02. Number of 14-byte mesh records at MeshBaseOffset.</summary>
    public ushort MeshCount { get; set; }

    /// <summary>Resolved mesh records (length = MeshCount).</summary>
    public List<MeshRecord> Meshes { get; set; } = new();

    /// <summary>Unique vertex arrays referenced by this LOD's mesh records.
    /// Multiple <see cref="MeshRecord"/>s frequently share the same pVertexArray near-pointer
    /// (verified: Z01 inn has 35 mesh records but only 2 unique vertex pools — one shared by
    /// 12 submeshes, one by 23). Each <see cref="MeshRecord.VertexPoolIndex"/> indexes into
    /// this list, so submeshes are face-and-sort-metadata groupings of a shared vertex pool.
    /// Across all 12 zones: 4009 mesh records → 1122 unique pools (~1.66 MB JSON saved).</summary>
    public List<List<Position3DShort>> VertexPools { get; set; } = new();
}

/// <summary>
/// Mesh record — 14-byte structure used inside an LOD's mesh array AND as child records
/// inside another mesh record (recursive). Consumed by renderShapeDispatcher (0x2a70c)
/// and built into a near-pointer array by sub_seg027_5D9 (0x2a6d9, stride 0xE).
/// See <c>docs/FileFormats/ZoneTable-DAT.md §1b</c>.
/// </summary>
public class MeshRecord
{
    /// <summary>+0x00. 0xFF = no runtime lookup (use MeshFaces[0]); else indexed into
    /// <c>word_seg024_3801[]</c> to fetch a flag byte that, modulo MeshFaceCount, picks
    /// the active mesh-face record.</summary>
    public byte RuntimeFlagsIndex { get; set; }

    /// <summary>+0x01. Index into this mesh's vertex pool (see <see cref="VertexPoolIndex"/>),
    /// used as the direction reference (rotation2) for the per-mesh depth-sort dot product
    /// computed by cullEntityForRender at 0x2a54f. Only consumed when the entity has
    /// <c>EntityFlags &amp; 0x40</c> (EF_DEPTH_SORTED) set; non-flagged entities never
    /// read this byte and pin it to 0xFF in shipped data. 0xFF on a flagged entity =
    /// skip this mesh's sort contribution (rotationDotProduct forced to 0).
    /// Empirical: 100% of in-range values fit `&lt; VertexCount` across 12 zones.</summary>
    public byte SortNormalVertex { get; set; }

    /// <summary>+0x02. Index into this mesh's vertex pool (see <see cref="VertexPoolIndex"/>),
    /// used as the anchor reference (rotation1, combined with a camera-related global offset)
    /// for the per-mesh depth-sort dot product. Read by cullEntityForRender at 0x2a4fa.
    /// Only consumed when the entity has <c>EntityFlags &amp; 0x40</c> set; non-flagged
    /// entities pin this to 0xFF. 0xFF on a flagged entity = use the global default
    /// anchor (stru_seg024_37DD) without per-vertex offset.</summary>
    public byte SortAnchorVertex { get; set; }

    /// <summary>+0x03. Number of vertices in this mesh's vertex pool. Used as <c>rep stosw</c>
    /// count to clear the per-mesh vertex-visited array (one word per vertex) at 0x23a77 etc.
    /// Equal to <c>LodLevel.VertexPools[VertexPoolIndex].Count</c> when a pool is referenced.</summary>
    public byte VertexCount { get; set; }

    /// <summary>+0x06. Number of 8-byte mesh-face records at PMeshFaceData. Modulus for the
    /// runtime-flag → record index mapping.</summary>
    public ushort MeshFaceCount { get; set; }

    /// <summary>+0x0A. Number of 14-byte child mesh records at PChildren. 0 = leaf.</summary>
    public ushort ChildCount { get; set; }

    /// <summary>Index into the parent <see cref="LodLevel.VertexPools"/> identifying which
    /// vertex pool this mesh references. -1 = no pool (mesh's pVertexArray was 0 or
    /// VertexCount was 0). Multiple meshes commonly share a pool; the original file's
    /// pVertexArray near-pointer is the dedup key (see ZoneTable-DAT.md §1b).</summary>
    public int VertexPoolIndex { get; set; } = -1;

    /// <summary>Resolved mesh-face record array (length = MeshFaceCount). Tagged union by RenderType.</summary>
    public List<MeshFaceRecord> MeshFaces { get; set; } = [];

    /// <summary>Resolved child mesh records (length = ChildCount, recursive).</summary>
    public List<MeshRecord> Children { get; set; } = [];
}

/// <summary>
/// Mesh-face record (8 bytes), tagged union keyed on <see cref="RenderType"/>.
/// The dispatcher reads only +0; everything else is per-variant.
/// </summary>
#if JSON_SERIALIZE
[JsonDerivedType(typeof(PolygonMeshFace), nameof(PolygonMeshFace))]
[JsonDerivedType(typeof(SpriteAMeshFace), nameof(SpriteAMeshFace))]
[JsonDerivedType(typeof(SpriteBMeshFace), nameof(SpriteBMeshFace))]
#endif
public abstract class MeshFaceRecord
{
    /// <summary>+0x00. 0 = polygon, 1 = sprite-A (vertex-anchored billboard), &gt;=2 = sprite-B (bitmap).</summary>
    public byte RenderType { get; set; }

    /// <summary>+0x01. Not read by dispatcher or any handler prolog. Possibly padding;
    /// preserved verbatim so we never silently drop bytes.</summary>
    public byte Unknown01 { get; set; }
}

/// <summary>RenderType == 0. Handled by processEntityFaces (0x23a48).</summary>
public class PolygonMeshFace : MeshFaceRecord
{
    /// <summary>+0x02. Number of polygon face records at PFaceArray.</summary>
    public ushort FaceCount { get; set; }

    /// <summary>+0x06. Not directly read on the polygon path. Possibly bounding-box seed (TO VERIFY).</summary>
    public ushort Unknown06 { get; set; }

    /// <summary>Resolved polygon face array (length = FaceCount).</summary>
    public List<PolygonFace> Faces { get; set; } = new();
}

/// <summary>RenderType == 1. Handled by renderSprite1 (0x235a0). Vertex-anchored billboard.</summary>
public class SpriteAMeshFace : MeshFaceRecord
{
    /// <summary>+0x02. Half-size; scaled with sar against runtime view scale to get sprite_w/sprite_h.</summary>
    public ushort HalfSize { get; set; }

    /// <summary>+0x04. Vertex used as billboard anchor.</summary>
    public byte VertexIndex { get; set; }

    /// <summary>+0x05. Pen color when VGA_byte_dseg_20DF != 0 (EGA/CGA).</summary>
    public byte ColorEga { get; set; }

    /// <summary>+0x06. Pen color when VGA_byte_dseg_20DF == 0 (VGA).</summary>
    public byte ColorVga { get; set; }

    /// <summary>+0x07. Not read by handler prolog.</summary>
    public byte Unknown07 { get; set; }
}

/// <summary>RenderType &gt;= 2. Handled by renderSprite2 (0x23031). Bitmap-anchored billboard.</summary>
public class SpriteBMeshFace : MeshFaceRecord
{
    /// <summary>+0x02. Index into pBitmapSlots[]; 0xFFFF triggers placeholder lookup.</summary>
    public ushort BitmapIndex { get; set; }

    /// <summary>+0x04. Not directly read by handler prolog.</summary>
    public byte Unknown04 { get; set; }

    /// <summary>+0x05. Not directly read by handler prolog.</summary>
    public byte Unknown05 { get; set; }

    /// <summary>+0x06. Not directly read by handler prolog.</summary>
    public byte Unknown06 { get; set; }

    /// <summary>+0x07. Vertex used as billboard anchor.</summary>
    public byte VertexIndex { get; set; }
}

/// <summary>
/// Polygon face record (8 bytes) — referenced by <see cref="PolygonMeshFace.PFaceArray"/>.
/// Verified against IDA: processEntityFaces (0x23a48), calculateFaceShading (0x248f4),
/// backfaceCullDotProduct (0x239d4). Bytes are palette indices; the engine's per-zone
/// Z##L.SCX strip system can substitute strip samples for selected pen colors at draw time
/// (see ZoneTable-DAT.md §4.5).
/// </summary>
public class PolygonFace
{
    /// <summary>+0x00. bits 0-1 = type (0=double-sided, 1/3=single-sided cull, 2=skip),
    /// bit 4 (0x10) = underground shadow highlight, bit 5 (0x20) = enable shading,
    /// bit 7 (0x80) = force flat drawColor=1 / skip shading. See ZoneTable-DAT.md §4.</summary>
    public byte Flags { get; set; }

    /// <summary>+0x01. Pen color used when VGA_byte_dseg_20DF == 0 (VGA mode).</summary>
    public byte VgaColor { get; set; }

    /// <summary>+0x02. Shade-table input + background color (VGA mode).</summary>
    public byte VgaShade { get; set; }

    /// <summary>+0x03. Pen color used when VGA_byte_dseg_20DF != 0 (EGA/CGA mode).</summary>
    public byte EgaColor { get; set; }

    /// <summary>+0x04. Shade-table input + background color (EGA/CGA mode).</summary>
    public byte EgaShade { get; set; }

    /// <summary>+0x05. Vertex index used for face-normal direction (backface cull).</summary>
    public byte NormalVertexIndex { get; set; }

    /// <summary>Resolved vertex-index list (terminator stripped).</summary>
    public List<int> VertexIndices { get; set; } = new();
}

/// <summary>
/// GID section entry — grid/collision info for a world item type.
/// </summary>
public class TableGidInfo
{
    public ushort XRadius { get; set; }
    public ushort YRadius { get; set; }
    public ushort Flags { get; set; }
    public List<TableGidCoord> TextureCoords { get; set; } = new();
    public List<TableGidCoord> OtherCoords { get; set; } = new();
    public ushort Unknown06 { get; set; }
    public byte Unknown09 { get; set; }
    public ushort Unknown0A { get; set; }
}

public class TableGidCoord
{
    public int X { get; set; }
    public int Y { get; set; }
}

/// <summary>
/// Short (16-bit signed) 3D position, used for TBL vertices.
/// </summary>
public class Position3DShort
{
    public short X { get; set; }
    public short Y { get; set; }
    public short Z { get; set; }
}
