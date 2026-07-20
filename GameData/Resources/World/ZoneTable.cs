namespace GameData.Resources.World;

using GameData.Resources.Data;

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

    /// <summary>Semantic interaction behavior key (null = non-interactable). Resolved from
    /// <see cref="TableDatInfo.EntityType"/> by ResourceExtraction.InteractionProfileTable.</summary>
    public string? Behavior { get; set; }

    /// <summary>Data-driven interaction profile (null = non-interactable).</summary>
    public InteractionProfile? Interaction { get; set; }
}


/// <summary>
/// MAP section of a Z##.TBL — author-time list of entity-type names (one per type).
/// The runtime engine never reads this section: KRONDOR.EXE contains only "GID:" and
/// "DAT:" tag literals, and the only callers of <c>LoadZoneTableTag</c> (0x31acb) are
/// <c>LoadZoneTableGidTag</c> (0x29ac9) and <c>LoadZoneTableDatTag</c> (0x31503). The
/// MAP layout therefore reflects the editor that produced the file, not the game.
///
/// On-disk layout (verified across all 16 shipped TBL files, 2026-04-28):
/// <code>
///   +0x00 char[4]                tag = "MAP:"
///   +0x04 u32                    sectionSize
///   +0x08 u16                    Capacity         (always == NumItems in shipped data)
///   +0x0A u16                    NumItems
///   +0x0C u16[NumItems]          offsets[]        (byte offsets into the string pool)
///   +0x0C+2*NumItems  u16        StringPoolSize   (= offsets[NumItems] sentinel)
///   +0x0E+2*NumItems  byte[StringPoolSize] strings (NUL-terminated, packed)
/// </code>
/// </summary>
public record MapSection
{
    public List<string> Names { get; set; }

    /// <summary>+0x00 of section data. Always equal to <see cref="NumItems"/> in shipped
    /// data — likely an editor-side capacity / max-count paired with a separate used-count.
    /// Preserved verbatim so a write-path could round-trip the file unchanged.</summary>
    public ushort Capacity { get; set; }

    /// <summary>End-of-string-pool sentinel: byte length of the string-data region that
    /// follows. Functionally equivalent to <c>offsets[NumItems]</c> — the runtime would
    /// compute the last string's length the same way as any other (offsets[i+1]-offsets[i]).
    /// Cross-file invariant: <c>sectionSize == 6 + 2*NumItems + StringPoolSize</c> in every
    /// shipped TBL.</summary>
    public ushort StringPoolSize { get; set; }
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

    /// <summary>Decoded <see cref="EntityFlags"/> 0x20 (EF_UNBOUNDED): the 12-byte bbox is omitted.
    /// The bit meaning is documented on <see cref="EntityFlags"/>; decoded here once so consumers never
    /// re-mask it.</summary>
    public bool IsUnbounded => (EntityFlags & 0x20) != 0;

    /// <summary>Decoded <see cref="EntityFlags"/> 0x40 (EF_DEPTH_SORTED): the entity takes the
    /// painter's-algorithm depth-sort render path.</summary>
    public bool IsDepthSorted => (EntityFlags & 0x40) != 0;

    /// <summary>+0x01. Entity-type tag.</summary>
    public WorldEntityType EntityType { get; set; }

    /// <summary>+0x02. Painter's-sort draw priority — the original's ONLY reader is the world-item
    /// depth sort (<c>depthSortVisibleItems</c>), not any movement/collision/terrain-surface logic
    /// (verified in IDA 2026-06-29). 8=ground/base, 7=road/path, 6=river, 0=non-terrain object;
    /// higher = painted first/under. Renamed from the misnomer <c>TerrainType</c>.</summary>
    public byte DrawPriority { get; set; }

    /// <summary>+0x03. On-disk shift count for vertex coords (RenderWorldItem 0x2a939 →
    /// g_vertexScale) — the format's 16-bit compression exponent.
    /// <b>Provenance only: do not apply it.</b> As of 2026-07-20 the extractor bakes this shift
    /// into <see cref="LodLevel.VertexPools"/>, <see cref="Min"/>/<see cref="Max"/> and
    /// <see cref="Extent"/>, so those ship in plain model space. Retained so a write-path can
    /// re-compress and so the original encoding stays legible. Note GID region coordinates are
    /// NOT baked — they keep their own space.</summary>
    public byte VertexScale { get; set; }

    /// <summary>+0x04. Purpose TBD. Verified 2026-06-02 NOT read by the render pipeline
    /// (RenderWorldItem/ComputeZoneDrawDistance) nor by any per-type-slot consumer of
    /// <c>pTblDataSlots</c> (load/free/swap/lookup). The 26-byte entity header is only walked by
    /// RenderWorldItem, which ignores +4/+6 — so any reader resolves the header by a different path.
    /// Non-zero only on ~20 <b>animated</b> entity types (boom/gate/m_door/catapult), so likely an
    /// animation parameter; confirming needs a Spice86 memory watchpoint (no static reader found).
    /// Observed: 0/1/2.</summary>
    public ushort Unknown04 { get; set; }

    /// <summary>+0x06. Purpose TBD. Same status as <see cref="Unknown04"/> — no static reader; non-zero
    /// only on the same ~20 animated entities; small per-zone-varying integers (animation
    /// speed/frame/target?). Confirm with a runtime watchpoint.</summary>
    public ushort Unknown06 { get; set; }

    /// <summary>+0x08. Number of LOD records at LodArrayOffset.</summary>
    public ushort LodCount { get; set; }

    /// <summary>+0x0C. Signed world-space entity extent for distance culling — the engine's
    /// <c>dword_3803</c> (RenderWorldItem 0x2a948: <c>movsx eax</c>, then shifted left by
    /// VertexScale). Stored pre-shifted (2026-07-20), hence 32-bit: consumers use it directly.</summary>
    public int Extent { get; set; }

    /// <summary>+0x0E..+0x13 (only when (EntityFlags &amp; 0x20) == 0 — bbox present).
    /// 3 i16: minX, minY, minZ. Read by computeEntityViewExtent (0x238d7).</summary>
#if JSON_SERIALIZE
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
    public Position3DInt? Min { get; set; }

    /// <summary>+0x14..+0x19 (only when (EntityFlags &amp; 0x20) == 0 — bbox present).
    /// 3 i16: maxX, maxY, maxZ. Read by computeEntityViewExtent (0x238d7).</summary>
#if JSON_SERIALIZE
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
    public Position3DInt? Max { get; set; }

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
    /// Across the 12 Z-zones (Z01–Z12): 4009 mesh records → 817 unique pools (4.9× dedup)
    /// — verified 2026-06-08 against generated/TBL output. (All 16 TBL files incl. COMBAT +
    /// M-variants: 4491 → 939.)</summary>
    public List<List<Position3DInt>> VertexPools { get; set; } = new();
}

/// <summary>
/// Mesh record — 14-byte structure used inside an LOD's mesh array AND as child records
/// inside another mesh record (recursive). Consumed by renderShapeDispatcher (0x2a70c)
/// and built into a near-pointer array by create_meshPointerArray (0x2a6d9, stride 0xE).
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

    /// <summary>+0x06. Structure padding. Verified 2026-06-02: none of the three polygon
    /// renderers (processEntityFaces 0x23a48, processEntityFaces16 0x2505a, processEntityFaces32
    /// 0x24e22) read it — they consume only +0x02 (FaceCount) and +0x04 (pFaceArray), then walk the
    /// face array. No runtime reader; safe to ignore in the port (preserved verbatim).
    /// Verified 2026-07-20: zero in all 3887 polygon mesh-face records across all 12 shipped
    /// zones, i.e. padding rather than an unknown field. Values previously extracted here were
    /// an artefact of reading +0x06 after the face-array walk had moved the shared cursor.</summary>
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

/// <summary>RenderType &gt;= 2. Handled by renderSprite2 (0x23031). Bitmap-anchored billboard.
///
/// Full read trace of renderSprite2 (2026-06-02): the dispatcher passes <c>&amp;record+2</c>
/// (renderShapeDispatcher 0x2a880: <c>add ax,2; call renderSprite2</c>), so every offset
/// below is the on-disk struct offset. The billboard is built as:
/// <code>
///   cx = SizeScale != 0 ? (SizeScale * viewScale) >> 8 : viewScale   // viewScale = word_seg024_380F
///   si = (cx << 10) / (max(texW, texH) / 2)                          // scale ratio
///   screenW = texW * si >> 10 ;  screenH = texH * si >> 10           // larger dim → ~2*cx px
///   xPos = proj(VertexIndex).x - (AnchorX * si >> 10)                // mirrored to (screenW-x) when bitmap h-flipped
///   yPos = proj(VertexIndex).y - (AnchorY * si >> 10)
/// </code>
/// So <see cref="SizeScale"/> is the world-space size of the billboard (the bitmap's own
/// pixel dimensions only set the aspect ratio), and (<see cref="AnchorX"/>, <see cref="AnchorY"/>)
/// is the hotspot inside the unscaled bitmap that lands on the projected anchor vertex.</summary>
public class SpriteBMeshFace : MeshFaceRecord
{
    /// <summary>+0x02. Index into pBitmapSlots[]; 0xFFFF triggers placeholder lookup. Read at 0x23066.</summary>
    public ushort BitmapIndex { get; set; }

    /// <summary>+0x04. Anchor/hotspot <b>Y</b> in unscaled source-bitmap pixels. Scaled by the
    /// sprite's <c>si</c> factor and subtracted from the projected anchor-vertex screen Y to get
    /// the bitmap's top edge (renderSprite2 0x231c2 low byte → 0x23260). Typically near the
    /// bottom of the frame (feet-on-ground); e.g. the gorath creature ships ~50–70.</summary>
    public byte AnchorY { get; set; }

    /// <summary>+0x05. Anchor/hotspot <b>X</b> in unscaled source-bitmap pixels. Scaled and
    /// subtracted from the projected anchor-vertex screen X (renderSprite2 0x231c2 high byte →
    /// 0x23254); mirrored to <c>screenW - x</c> when the bitmap is horizontally flipped
    /// (bitmap_flags_048E != 0, 0x231f5). Typically ≈ width/2 (horizontal centre).</summary>
    public byte AnchorX { get; set; }

    /// <summary>+0x06. Billboard size as a fraction of the entity's own world extent. The
    /// sprite's larger <b>world</b> dimension is <c>SizeScale / 128 × (Extent &lt;&lt; VertexScale)</c>
    /// (the texture's pixel dimensions only set the aspect ratio). <c>0</c> ⇒ use the entity
    /// extent directly at 2× (equivalent to SizeScale 256).
    ///
    /// Derivation (renderSprite2 0x23031 + RenderWorldItem 0x2a95a): <c>cx = (SizeScale * word_380F) >> 8</c>
    /// and the bitmap's larger dim is scaled to <c>2*cx</c> px; <c>word_380F</c> is the entity's
    /// extent (<c>g_entityWorldExtent = Extent &lt;&lt; VertexScale</c>) projected to screen by
    /// ComputeZoneDrawDistance, so the projection cancels and the result is depth-independent.
    /// No runtime constant required. Observed range 0–255, clustered around 100–130.</summary>
    public byte SizeScale { get; set; }

    /// <summary>+0x07. Vertex used as billboard anchor (projected to screen, read at 0x230b4).</summary>
    public byte VertexIndex { get; set; }

    /// <summary>Resolved, self-describing texture reference baked at extraction: the resource key
    /// (e.g. "Z01SLOT3.BMX#8") for this billboard's bitmap (<see cref="BitmapIndex"/> resolved against
    /// the zone's concatenated slot bitmaps), or <c>null</c> when out of range.</summary>
    public string? TextureBitmap { get; set; }
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
    /// bit 7 (0x80) = force flat drawColor=1 / skip shading. See ZoneTable-DAT.md §4.
    /// Prefer <see cref="CullMode"/> over re-masking this byte.</summary>
    public byte Flags { get; set; }

    /// <summary>Bits 0-1 of <see cref="Flags"/> as a semantic value, so consumers never mask the
    /// raw byte. Bits 1 and 3 both mean single-sided; the remaining flag bits are unrelated
    /// (texturing/shading) and are deliberately ignored here.</summary>
    public FaceCullMode CullMode => (Flags & 0x03) switch
    {
        0 => FaceCullMode.DoubleSided,
        2 => FaceCullMode.Skip,
        _ => FaceCullMode.SingleSided,
    };

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

    /// <summary>Resolved, self-describing texture reference baked at extraction: the resource key
    /// (e.g. "Z01SLOT3.BMX#8") of the slot bitmap this textured quad uses, or <c>null</c> when the
    /// face is flat/terrain (not a Flags&amp;0x10 quad, or the index is out of range). Decouples model
    /// texturing from the zone's runtime bitmap load order (see the 2026-07-16 design doc).</summary>
    public string? TextureBitmap { get; set; }
}

/// <summary>
/// GID section entry — collision and elevation grid for a world item type.
/// Consumed at runtime by <c>CheckMoveDestination</c> (0x72333) via the chain
/// <c>sub_seg026_92 → sub_seg026_108 → sub_seg026_228 → sub_seg026_419 / sub_seg026_59D</c>.
/// See <c>docs/FileFormats/ZoneTable-DAT.md §0b</c>.
///
/// On-disk header (8 bytes), followed by <see cref="Regions"/> immediately at gid+8:
///   +0x00 i16 XRadius   — half-extent on local X (rotated bbox check)
///   +0x02 i16 YRadius   — half-extent on local Y
///   +0x04 u8  Flags     — bit 0x01 = "shortcut" hit-test (never set in shipped data)
///                          bit 0x02 = sloped regions (region stride 10 vs 6)
///   +0x05 u8  regionCount (= <c>Regions.Count</c>)
///   +0x06 u16 pRegions   — segment-relative pointer; in shipped data always (segOfs+8)
/// </summary>
public class TableGidInfo
{
    /// <summary>+0x00. Half-extent on local X. Bbox-rejects the rotated query point. Read at IDA 0x29d7e.</summary>
    public short XRadius { get; set; }

    /// <summary>+0x02. Half-extent on local Y. Read at IDA 0x29d8a.</summary>
    public short YRadius { get; set; }

    /// <summary>+0x04. Flag bits: <c>0x01</c> = shortcut hit-test (skip per-region polygon walk
    /// and reuse previous frame's matched region; tested at 0x29dc5; never set in shipped data).
    /// <c>0x02</c> = sloped regions (stride 10, with slope-correction; tested at 0x29daa).
    /// Bits 0x04..0x80 have no readers and are never set in shipped data.
    /// Prefer <see cref="IsSloped"/> over re-masking this byte.</summary>
    public byte Flags { get; set; }

    /// <summary>Bit 1 (<c>0x02</c>) of <see cref="Flags"/>: this entity's regions carry slope
    /// planes and use the 10-byte stride rather than the flat 6-byte one.</summary>
    public bool IsSloped => (Flags & 0x02) != 0;

    /// <summary>Resolved region records (length = on-disk regionCount at +0x05).
    /// Each region is a convex polygon with one elevation; the first region whose polygon
    /// contains the query point wins. See <see cref="GidRegion"/>.</summary>
    public List<GidRegion> Regions { get; set; } = new();
}

/// <summary>
/// Single convex polygon at one elevation. On-disk size depends on
/// <c>TableGidInfo.Flags &amp; 0x02</c>:
///
/// Flat region (6 bytes):
///   +0x00 u16 pSubedges
///   +0x02 u8  subedgeCount
///   +0x03 u8  _pad        (always 0 in shipped data)
///   +0x04 i16 BaseElevation
///
/// Sloped region (10 bytes):
///   +0x00 u16 pSubedges
///   +0x02 u8  subedgeCount
///   +0x03 u8  SlopeShift
///   +0x04 i16 BaseElevation
///   +0x06 u16 pSlopePlane
///   +0x08 u8  SlopeMagnitude (editor-computed slope-steepness summary)
///   +0x09 u8  SlopeBearing  (binary compass angle of downhill direction, 0..255)
/// </summary>
public class GidRegion
{
    /// <summary>+0x04 (i16). Z value at the slope's anchor when the query point lies inside
    /// this region. Read at IDA 0x29eaa (flat) and 0x2a100 (sloped). The runtime then
    /// shifts by <c>worldItem.shiftScale</c> and adds <c>worldItem.z</c>.
    /// Extracted value carries the ×1.2 world-up aspect bake applied to DAT vertices and bboxes
    /// (see ZoneTableExtractor.WorldUpAspectScale) — this is a world-up Z, so it must stretch with
    /// the geometry it describes. Added 2026-07-20; previously shipped unscaled, leaving every
    /// elevation 20% below its own surface.</summary>
    public short BaseElevation { get; set; }

    /// <summary>+0x03 (u8). Slope-correction scale exponent. 0 = no slope contribution.
    /// Only meaningful when the parent's <c>Flags &amp; 0x02</c>; on flat regions this byte is
    /// padding (always 0). Read at IDA 0x2a0e5 (<c>movzx eax, byte ptr es:[bx+3]</c>).
    /// Observed range 0..0x24.</summary>
    public byte SlopeShift { get; set; }

    /// <summary>+0x08 (u8, sloped only). Editor-computed slope-steepness summary.
    /// Correlates with <c>slopeShift × |slope| × polygon_extent</c>; for ramps with
    /// fixed slope=64 the value matches <c>round(slopeShift × 64 / 100)</c> exactly
    /// (e.g. bridge ramps: shift=3→2, shift=11→7), but the polygon-dependent factor
    /// keeps it from collapsing to a closed form across all entities.
    /// **No runtime reader.** Preserved verbatim. <c>null</c> on flat regions.</summary>
    public byte? SlopeMagnitude { get; set; }

    /// <summary>+0x09 (u8, sloped only). Editor-computed compass bearing of this region's
    /// downhill direction, encoded as a binary angle in `1/256` turns:
    /// <c>bearing = round(atan2(a, -b) × 256 / 360°) mod 256</c>. Verified against all 16
    /// shipped sloped regions: 0=south, 0x40=west, 0x80=north, 0xC0=east. Z06's
    /// <c>bridge1</c> ships <c>0xA0</c> (NNW) instead of <c>0x80</c> (north) because its
    /// slope plane is geometrically rotated, not because of zone-specific scripting.
    /// **No runtime reader.** Preserved verbatim. <c>null</c> on flat regions.</summary>
    public byte? SlopeBearing { get; set; }

    /// <summary>Resolved subedge array (length = on-disk subedgeCount, ≤ 32). Each subedge
    /// is a half-plane; the polygon containing this region is their intersection.
    /// See <see cref="GidSubedge"/>.</summary>
    public List<GidSubedge> Subedges { get; set; } = new();

    /// <summary>Resolved slope plane (sloped regions only; <c>null</c> on flat).
    /// See <see cref="GidSlopePlane"/>.</summary>
    public GidSlopePlane? Slope { get; set; }
}

/// <summary>
/// One half-plane of a region's convex polygon (6 bytes).
/// Hit test (sub_seg026_419, 0x29ed9) treats the polygon as the intersection of
/// every subedge's "inside" side, using sign comparisons of cross-products of
/// <c>(Dx, Dy) × (P - Anchor)</c>.
/// </summary>
public class GidSubedge
{
    /// <summary>+0x00 (i8). Edge direction X. Typically ±100 or ±125 for unit-ish vectors.</summary>
    public sbyte Dx { get; set; }

    /// <summary>+0x01 (i8). Edge direction Y.</summary>
    public sbyte Dy { get; set; }

    /// <summary>+0x02 (i16). A point on the edge — usually a polygon vertex.</summary>
    public short AnchorX { get; set; }

    /// <summary>+0x04 (i16).</summary>
    public short AnchorY { get; set; }
}

/// <summary>
/// Slope plane referenced by a sloped region (6 bytes). Used by sub_seg026_59D (0x2a05d):
/// <code>
///   elevation(P) = BaseElevation +
///                  ((A * (AnchorX - P.x) + B * (AnchorY - P.y)) * SlopeShift) >> 12
/// </code>
/// </summary>
public class GidSlopePlane
{
    /// <summary>+0x00 (i8). X gradient coefficient. Carries the ×1.2 world-up aspect bake
    /// (2026-07-20): the gradient term is a Z value like BaseElevation, so scaling only the base
    /// would pivot the ramp about its anchor instead of stretching it. Quantisation across the 95
    /// shipped sloped regions is ≤1.5%.</summary>
    public sbyte A { get; set; }

    /// <summary>+0x01 (i8). Y gradient coefficient. Scaled with <see cref="A"/>; scaling both
    /// preserves their ratio, so the verbatim SlopeBearing stays consistent (worst drift 0.40°,
    /// below the bearing byte's own 1.4° resolution).</summary>
    public sbyte B { get; set; }

    /// <summary>+0x02 (i16). Reference X (slope's "zero point" on X).</summary>
    public short AnchorX { get; set; }

    /// <summary>+0x04 (i16).</summary>
    public short AnchorY { get; set; }
}

/// <summary>
/// Short (16-bit signed) 3D position, used for TBL vertices.
/// </summary>
/// <summary>
/// A model-space coordinate triple from the TBL DAT section. 32-bit rather than 16-bit because
/// the extractor bakes the per-entity VertexScale exponent into the stored values (2026-07-20):
/// that exponent is the format's own 16-bit compression, so decompressed coordinates do not fit
/// in a short — 64 shipped axis-values exceed it, peaking at 51200.
/// </summary>
public class Position3DInt
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
}
