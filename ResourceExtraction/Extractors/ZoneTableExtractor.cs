using System;

namespace ResourceExtraction.Extractors;

using GameData.Resources.Data;
using GameData.Resources.Image;
using GameData.Resources.World;
using ResourceExtraction.World;
using Extensions;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Extracts Z##.TBL files — tagged resource files containing world item type definitions.
///
/// TBL format (4 tagged sections):
///   MAP: name string table (one per entity type)
///   APP: application data (skipped)
///   GID: grid/collision data per entity type
///   DAT: rendering data — geometry and sprite references
///
/// DAT-section layout on disk (verified against IDA: LoadZoneTableDatTag 0x31503,
/// RenderWorldItem 0x2a89f, renderShapeDispatcher 0x2a70c, sub_seg027_5D9 0x2a6d9,
/// computeEntityViewExtent 0x238d7, sub_seg027_2BB 0x2a3bb —
/// see docs/FileFormats/ZoneTable-DAT.md §1a/§1b/§1c):
///
///   1. Top-level pointer table — array of 4-byte (lower:u16, upper:u16) pairs.
///      Each entity's flat byte offset within DAT = (upper &lt;&lt; 4) + (lower &amp; 0xF).
///      Each entity's segment base for resolving internal near-pointers = (upper &lt;&lt; 4).
///      Terminated by (0, 0). LoadZoneTableDatTag relocates these in-place.
///
///   2. Top-level entity (14 bytes) — TableDatInfo:
///        +0x00 u8  entityFlags       (bit 0x20 EF_UNBOUNDED → no bbox; bit 0x40 EF_DEPTH_SORTED → per-mesh painter's-algorithm sort)
///        +0x01 u8  entityType
///        +0x02 u8  drawPriority      (painter's-sort layer; renamed from misnomer terrainType)
///        +0x03 u8  vertexScale       (shift count for vertex coords + extent)
///        +0x04 u16 unknown04         (interaction param? non-zero only on ~20 interactive entities)
///        +0x06 u16 unknown06         (interaction param? same)
///        +0x08 u16 lodCount
///        +0x0A u16 lodArrayOffset    (segment-relative)
///        +0x0C i16 extent            (signed; world-size = extent &lt;&lt; vertexScale)
///
///   2b. Bbox (12 bytes, ONLY when (entityFlags &amp; 0x20) == 0):
///        +0x0E i16 minX
///        +0x10 i16 minY
///        +0x12 i16 minZ
///        +0x14 i16 maxX
///        +0x16 i16 maxY
///        +0x18 i16 maxZ
///       Read by computeEntityViewExtent (0x238ea-0x239ac) for view-extent culling.
///
///   3. LOD records (6 bytes each, lodCount of them, at LodArrayOffset):
///        +0x00 u16 threshold
///        +0x02 u16 meshCount
///        +0x04 u16 meshBaseOffset    (segment-relative)
///
///   4. Mesh records (14 bytes each, meshCount of them, at MeshBaseOffset; stride 0xE):
///        +0x00 u8  runtimeFlagsIndex (0xFF = no runtime lookup)
///        +0x01 u8  sortNormalVertex  (vertex index for depth-sort dot product; 0xFF = skip; only read when EF_DEPTH_SORTED)
///        +0x02 u8  sortAnchorVertex  (vertex index used as anchor reference; 0xFF = use global default)
///        +0x03 u8  vertexCount
///        +0x04 u16 pVertexArray     (segment-relative)
///        +0x06 u16 meshFaceCount
///        +0x08 u16 pMeshFaceData    (segment-relative)
///        +0x0A u16 childCount
///        +0x0C u16 pChildren        (segment-relative; recursive)
///
///   5. Mesh-face records (8 bytes, tagged union by +0; see §2 of docs).
///   6. Polygon face records (8 bytes; see §3 of docs).
///
/// All segment-relative pointers within an entity's tree share the same segment base
/// = (upper &lt;&lt; 4) from the top-level pointer-table entry. To resolve any internal
/// pointer P to a flat DAT byte offset: <c>(upper &lt;&lt; 4) + P</c>.
/// </summary>
public class ZoneTableExtractor : ExtractorBase<ZoneTable>
{
    // entityFlags bit definitions (verified 2026-04-27 against IDA disasm):
    //   0x20 = EF_UNBOUNDED    — when SET, the 12-byte bbox is omitted.
    //   0x40 = EF_DEPTH_SORTED — when SET, cullEntityForRender (0x2a3bb) computes a
    //          per-mesh dot product from MeshRecord.SortNormalVertex / SortAnchorVertex
    //          (mesh-record bytes +1/+2) and reorders the mesh-pointer array
    //          (painter's algorithm). When CLEAR, those two bytes are unread (always
    //          0xFF in shipped data on non-flagged entities).
    private const byte EF_UNBOUNDED = 0x20;

    // Non-square-pixel (1.2) aspect bake. The original 320×200 mode-13h image was shown on a
    // 4:3 CRT, so pixels were 1.2× taller than wide and the 3D renderer applied NO compensation
    // (one focal constant for X and Y — verified in IDA: projectPoint32/perspectiveDivide32).
    // Vertices were therefore authored to be viewed THROUGH that vertical stretch: a house meant
    // to read as 2:1 was stored as 2.4:1 raw coords. Rendering raw coords on modern square pixels
    // makes everything ~20% too squat, so we bake the 1.2 back into the world-up axis here.
    //
    // World-up is BaK Z (BakCoordinateConverter: Unity.Y = BaK.Z). Ground-plane X/Y are left
    // literal so movement, footprint collision and pathing are untouched.
    //
    // This lives in the extractor on purpose: original-game models get the authored-CRT look,
    // while mod models bypass the extractor (straight through the converter/loader in square-pixel
    // space) and keep true proportions. See memory project-3d-aspect-camera / project-aspect-correction.
    // NEVER do this as a viewport/screen-space stretch — the remake's camera can pitch, which would
    // shear geometry pitch-dependently; a world-space per-vertex bake is the only faithful form.
    private const double WorldUpAspectScale = 1.2;

    private static int ScaleWorldUp(int z)
    {
        long scaled = (long)Math.Round(z * WorldUpAspectScale, MidpointRounding.AwayFromZero);
        if (scaled > int.MaxValue) scaled = int.MaxValue;
        else if (scaled < int.MinValue) scaled = int.MinValue;
        return (int)scaled;
    }

    /// <summary>
    /// Bake the per-entity VertexScale exponent into a model-space coordinate triple, then apply
    /// the world-up aspect scale to Z. The shift is applied FIRST so the ×1.2 rounds once, on the
    /// final magnitude, rather than compounding a rounding error already taken at raw scale.
    /// Baking here means <see cref="TableDatInfo.VertexScale"/> is provenance only — no consumer
    /// needs to know the format stored coordinates compressed.
    /// </summary>
    /// <summary>Read three consecutive on-disk i16 coordinates, unbaked.</summary>
    private static Position3DInt ReadRawTriple(BinaryReader reader) => new()
    {
        X = reader.ReadInt16(),
        Y = reader.ReadInt16(),
        Z = reader.ReadInt16(),
    };

    private static Position3DInt BakeModelSpace(Position3DInt raw, byte vertexScale)
    {
        int factor = 1 << vertexScale;
        raw.X *= factor;
        raw.Y *= factor;
        raw.Z = ScaleWorldUp(raw.Z * factor);
        return raw;
    }

    /// <summary>
    /// World-up bake for a GID slope-plane gradient coefficient. The elevation of a sloped region
    /// is <c>Base + ((A·dx + B·dy) · SlopeShift) &gt;&gt; 12</c> — the gradient term is a Z value like
    /// Base, so both must be scaled or the ramp pivots about its anchor instead of stretching.
    /// A/B are scaled (rather than SlopeShift) because they carry far more headroom: across the 95
    /// shipped sloped regions this quantises the slope by ≤1.5%, versus ≤11.1% via SlopeShift.
    /// Scaling both coefficients keeps their ratio, so the verbatim SlopeBearing (atan2(A, −B))
    /// stays consistent — measured worst-case drift 0.40°, below the 1.4° resolution of the
    /// byte-encoded bearing itself.
    /// </summary>
    private static sbyte ScaleWorldUp(sbyte gradient)
    {
        long scaled = (long)Math.Round(gradient * WorldUpAspectScale, MidpointRounding.AwayFromZero);
        if (scaled > sbyte.MaxValue) scaled = sbyte.MaxValue;
        else if (scaled < sbyte.MinValue) scaled = sbyte.MinValue;
        return (sbyte)scaled;
    }

    public override ZoneTable Extract(string id, Stream resourceStream)
    {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var table = new ZoneTable(id);

        var sections = ParseSectionHeaders(reader);

        if (!sections.ContainsKey("MAP") || !sections.ContainsKey("DAT"))
            throw new InvalidDataException($"TBL file {id} missing required MAP or DAT sections");

        table.MapSection = ParseMapSection(reader, sections["MAP"]);
        var names = table.MapSection.Names;
        int numItems = names.Count;

        var gidItems = sections.TryGetValue("GID", out var section)
            ? ParseGidSection(reader, section, numItems)
            : [];

        var datItems = ParseDatSection(reader, sections["DAT"], numItems, sections["DAT"].size);

        for (var i = 0; i < numItems; i++)
        {
            table.Entries.Add(new ZoneTableEntry
            {
                Index = i,
                Name = names[i],
                Dat = i < datItems.Count ? datItems[i] : new TableDatInfo(),
                Gid = i < gidItems.Count ? gidItems[i] : new TableGidInfo()
            });
        }

        StampInteraction(table);

        return table;
    }

    /// <summary>Stamp each entry's semantic Behavior + InteractionProfile from its
    /// EntityType byte (InteractionProfileTable). Runs in both the offline extractor and the
    /// Unity runtime load, since both call Extract.</summary>
    public static void StampInteraction(ZoneTable table) {
        foreach (ZoneTableEntry entry in table.Entries) {
            if (InteractionProfileTable.TryGet(entry.Dat.EntityType, out string behavior, out InteractionProfile profile)) {
                entry.Behavior = behavior;
                entry.Interaction = profile;
            }
        }
    }

    /// <summary>Bake a self-describing texture resource key onto every textured face, replacing the
    /// raw global bitmap index with a stable "Z##SLOT#.BMX#i" reference. Polygon quads use the
    /// Flags&amp;0x10 + quad rule (<see cref="SlotFaceResolver"/>); sprite-B faces resolve their raw
    /// BitmapIndex directly (same concatenation, no gate). Out-of-range / flat / non-quad ⇒ null.
    /// Mirrors <see cref="StampInteraction"/> as a post-parse pass so Extract stays single-stream.</summary>
    public static void StampTextureKeys(ZoneTable table, int zoneNumber, IReadOnlyList<int> slotImageCounts) {
        var index = new ZoneSlotBitmapIndex(slotImageCounts);
        foreach (var entry in table.Entries) {
            var dat = entry.Dat;
            if (dat?.Lods == null) continue;
            foreach (var lod in dat.Lods)
                foreach (var mesh in lod.Meshes)
                    StampMesh(mesh, zoneNumber, index);
        }
    }

    private static void StampMesh(MeshRecord mesh, int zoneNumber, ZoneSlotBitmapIndex index) {
        foreach (var mf in mesh.MeshFaces) {
            switch (mf) {
                case PolygonMeshFace poly:
                    foreach (var face in poly.Faces) {
                        SlotBitmapRef? r = SlotFaceResolver.Resolve(
                            face.Flags, face.VgaColor, face.VertexIndices.Count, index);
                        face.TextureBitmap = r.HasValue ? FormatSlotKey(zoneNumber, r.Value) : null;
                    }
                    break;
                case SpriteBMeshFace sprite:
                    sprite.TextureBitmap = index.TryResolve(sprite.BitmapIndex, out var sr)
                        ? FormatSlotKey(zoneNumber, sr) : null;
                    break;
            }
        }
        foreach (var child in mesh.Children) StampMesh(child, zoneNumber, index);
    }

    private static string FormatSlotKey(int zoneNumber, SlotBitmapRef r) =>
        $"Z{zoneNumber:D2}SLOT{r.SlotFile}.BMX#{r.LocalImage}";

    /// <summary>Runtime overload: gather the zone's slot-bitmap image counts from a resource provider
    /// (pass the BASE archive so keys are frozen against the original bitmaps — override BMX must not
    /// shift them) and stamp texture keys. Mirrors the CLI path but reads counts from the archive.</summary>
    public static void StampTextureKeys(ZoneTable table, string zoneTableId, IResourceProvider slotBitmapProvider) {
        if (ParseZoneNumber(zoneTableId) is not int zone) return;
        var counts = new List<int>();
        for (int slot = 0; slot < 7; slot++) {
            // Probe existence first (matches the CLI's ascending-until-missing load loop and
            // ChapterCatalogBuilder's CanProvideResource style). A genuine parse/IO error on a slot
            // that DOES exist then propagates instead of being swallowed — silent count truncation
            // would mis-texture with no log, the exact failure this task exists to fix.
            string slotId = $"Z{zone:D2}SLOT{slot}.BMX";
            if (!slotBitmapProvider.CanProvideResource(slotId)) break;   // no more slot files for this zone
            var set = slotBitmapProvider.GetResource<ImageSet>(slotId);
            if (set?.Images == null) break;
            counts.Add(set.Images.Count);
        }
        StampTextureKeys(table, zone, counts);
    }

    /// <summary>Leading zone number of a TBL id ("Z01.TBL"/"Z01" → 1, "Z10M.TBL" → 10); null for non-zone
    /// tables (COMBAT.TBL).</summary>
    public static int? ParseZoneNumber(string fileName) {
        var m = System.Text.RegularExpressions.Regex.Match(
            fileName, @"^Z(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success ? int.Parse(m.Groups[1].Value) : (int?)null;
    }

    private static Dictionary<string, (long offset, uint size)> ParseSectionHeaders(BinaryReader reader)
    {
        var sections = new Dictionary<string, (long offset, uint size)>();
        while (reader.BaseStream.Position < reader.BaseStream.Length - 8)
        {
            var tag = reader.ReadTag();
            var size = reader.ReadUInt32();
            if (string.IsNullOrEmpty(tag) || size == 0)
                break;
            sections[tag] = (reader.BaseStream.Position, size);
            reader.BaseStream.Seek(size, SeekOrigin.Current);
        }

        return sections;
    }

    // MAP section is editor-generated metadata never read by the runtime engine —
    // KRONDOR.EXE has no "MAP:" tag literal and no LoadZoneTableMapTag function. See
    // GameData/Resources/World/ZoneTable.cs MapSection for the full on-disk layout.
    private static MapSection ParseMapSection(BinaryReader reader, (long offset, uint size) section)
    {
        reader.BaseStream.Seek(section.offset, SeekOrigin.Begin);
        var mapSection = new MapSection
        {
            Names = [],
            Capacity = reader.ReadUInt16()
        };

        ushort numItems = reader.ReadUInt16();
        var offsets = new ushort[numItems];
        for (int i = 0; i < numItems; i++)
            offsets[i] = reader.ReadUInt16();

        mapSection.StringPoolSize = reader.ReadUInt16();
        long dataStart = reader.BaseStream.Position;

        for (int i = 0; i < numItems; i++)
        {
            reader.BaseStream.Seek(dataStart + offsets[i], SeekOrigin.Begin);
            mapSection.Names.Add(reader.ReadZeroTerminatedString());
        }

        return mapSection;
    }

    /// <summary>
    /// Parse the GID section. Layout (verified 2026-04-29 against IDA: LoadZoneTableGidTag
    /// 0x29ac9, sub_seg026_92/108/228/419/59D — see docs/FileFormats/ZoneTable-DAT.md §0b):
    ///
    ///   1. Pointer table — same encoding as DAT: numItems × 4 bytes (lower:u16, upper:u16).
    ///      Flat byte offset within GID = (upper &lt;&lt; 4) + (lower &amp; 0xF).
    ///      Segment base for resolving the entity's near-pointers = (upper &lt;&lt; 4).
    ///      Offset of the GID record within its segment = (lower &amp; 0xF).
    ///
    ///   2. Per typeId — gidEntityHeader (8 bytes): XRadius, YRadius, flags, regionCount, pRegions.
    ///
    ///   3. Region array (immediately at gid+8 in shipped data; reachable via pRegions in general):
    ///      stride 6 when (flags &amp; 2) == 0, stride 10 when set.
    ///
    ///   4. Subedges and slope planes are reachable through each region's near-pointers.
    /// </summary>
    private static List<TableGidInfo> ParseGidSection(BinaryReader reader, (long offset, uint size) section,
        int numItems)
    {
        reader.BaseStream.Seek(section.offset, SeekOrigin.Begin);
        var items = new List<TableGidInfo>(numItems);

        var entityFlatOffsets = new uint[numItems];
        var entitySegmentBases = new uint[numItems];
        for (int i = 0; i < numItems; i++)
        {
            ushort lower = reader.ReadUInt16();
            ushort upper = reader.ReadUInt16();
            entityFlatOffsets[i] = ((uint)upper << 4) + (uint)(lower & 0x000f);
            entitySegmentBases[i] = (uint)upper << 4;
        }

        for (int i = 0; i < numItems; i++)
        {
            items.Add(ParseGidEntity(reader, section.offset, entityFlatOffsets[i], entitySegmentBases[i],
                section.size));
        }

        return items;
    }

    private static TableGidInfo ParseGidEntity(BinaryReader reader, long sectionBase, uint entityOffset,
        uint segmentBase, uint sectionSize)
    {
        reader.BaseStream.Seek(sectionBase + entityOffset, SeekOrigin.Begin);
        var gid = new TableGidInfo
        {
            XRadius = reader.ReadInt16(),
            YRadius = reader.ReadInt16(),
        };
        gid.Flags = reader.ReadByte();
        byte regionCount = reader.ReadByte();
        ushort pRegions = reader.ReadUInt16();

        if (regionCount == 0)
            return gid;

        bool sloped = gid.IsSloped;
        int regionStride = sloped ? 10 : 6;

        for (int r = 0; r < regionCount; r++)
        {
            long regionPos = sectionBase + segmentBase + pRegions + r * regionStride;
            if (regionPos + regionStride > sectionBase + sectionSize)
                break;

            reader.BaseStream.Seek(regionPos, SeekOrigin.Begin);
            ushort pSubedges = reader.ReadUInt16();
            byte subedgeCount = reader.ReadByte();
            byte slopeShiftOrPad = reader.ReadByte();
            short baseElevation = reader.ReadInt16();

            var region = new GidRegion
            {
                // ×1.2 world-up aspect bake, matching the per-vertex/bbox bake (WorldUpAspectScale).
                // GID elevations describe the same world-up axis as the geometry they sit under.
                // Cast is safe: elevations are ground-height values, not vertex-scaled — the
                // largest shipped |BaseElevation| is 250, so ×1.2 stays far inside short range.
                BaseElevation = (short)ScaleWorldUp(baseElevation),
                SlopeShift = sloped ? slopeShiftOrPad : (byte)0,
            };

            if (sloped)
            {
                ushort pSlopePlane = reader.ReadUInt16();
                region.SlopeMagnitude = reader.ReadByte();   // +0x08
                region.SlopeBearing = reader.ReadByte();     // +0x09
                region.Slope = ReadSlopePlane(reader, sectionBase, segmentBase, sectionSize, pSlopePlane);
            }

            ReadSubedges(reader, region, sectionBase, segmentBase, sectionSize, pSubedges, subedgeCount);
            gid.Regions.Add(region);
        }

        return gid;
    }

    private static void ReadSubedges(BinaryReader reader, GidRegion region, long sectionBase, uint segmentBase,
        uint sectionSize, ushort pSubedges, byte subedgeCount)
    {
        if (pSubedges == 0 || subedgeCount == 0)
            return;
        for (int s = 0; s < subedgeCount; s++)
        {
            long pos = sectionBase + segmentBase + pSubedges + s * 6L;
            if (pos + 6 > sectionBase + sectionSize)
                break;
            reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            region.Subedges.Add(new GidSubedge
            {
                Dx = reader.ReadSByte(),
                Dy = reader.ReadSByte(),
                AnchorX = reader.ReadInt16(),
                AnchorY = reader.ReadInt16(),
            });
        }
    }

    private static GidSlopePlane? ReadSlopePlane(BinaryReader reader, long sectionBase, uint segmentBase,
        uint sectionSize, ushort pSlopePlane)
    {
        if (pSlopePlane == 0)
            return null;
        long pos = sectionBase + segmentBase + pSlopePlane;
        if (pos + 6 > sectionBase + sectionSize)
            return null;
        reader.BaseStream.Seek(pos, SeekOrigin.Begin);
        return new GidSlopePlane
        {
            // Gradient scaled with BaseElevation — see the sbyte ScaleWorldUp overload.
            // AnchorX/Y are ground-plane coordinates and are deliberately left literal.
            A = ScaleWorldUp(reader.ReadSByte()),
            B = ScaleWorldUp(reader.ReadSByte()),
            AnchorX = reader.ReadInt16(),
            AnchorY = reader.ReadInt16(),
        };
    }

    private static List<TableDatInfo> ParseDatSection(BinaryReader reader, (long offset, uint size) section,
        int numItems, long datSize)
    {
        reader.BaseStream.Seek(section.offset, SeekOrigin.Begin);
        var items = new List<TableDatInfo>();

        // Top-level pointer table: numItems × 4 bytes (lower:u16, upper:u16).
        // Decoded flat byte offset within DAT = (upper << 4) + (lower & 0xF).
        // Segment base for that entity's internal pointers = (upper << 4).
        var entityOffsets = new uint[numItems];
        var segmentBases = new uint[numItems];
        for (int i = 0; i < numItems; i++)
        {
            ushort lower = reader.ReadUInt16();
            ushort upper = reader.ReadUInt16();
            entityOffsets[i] = ((uint)upper << 4) + (uint)(lower & 0x000f);
            segmentBases[i] = (uint)upper << 4;
        }

        for (int i = 0; i < numItems; i++)
        {
            items.Add(ParseEntity(reader, section.offset, entityOffsets[i], segmentBases[i], datSize));
        }

        return items;
    }

    /// <summary>
    /// Parse a top-level 14-byte entity header and walk its LOD records.
    /// </summary>
    private static TableDatInfo ParseEntity(BinaryReader reader, long datBase, uint entityOffset, uint segmentBase,
        long datSize)
    {
        reader.BaseStream.Seek(datBase + entityOffset, SeekOrigin.Begin);
        var dat = new TableDatInfo();
        dat.EntityFlags = reader.ReadByte();
        dat.EntityType = (WorldEntityType)reader.ReadByte();
        dat.DrawPriority = reader.ReadByte();
        dat.VertexScale = reader.ReadByte();
        dat.Unknown04 = reader.ReadUInt16();
        dat.Unknown06 = reader.ReadUInt16();
        dat.LodCount = reader.ReadUInt16();
        var lodArrayOffset = reader.ReadUInt16();
        // Engine's dword_3803 == Extent << VertexScale; ship that, not the operands.
        dat.Extent = reader.ReadInt16() << dat.VertexScale;

        // Bbox follows the 14-byte header iff EF_UNBOUNDED is clear. Reader is positioned at +0x0E.
        if ((dat.EntityFlags & EF_UNBOUNDED) == 0)
        {
            // Baked identically to the vertices it bounds, so view-extent culling still covers
            // the geometry after both the vertex-scale and world-up bakes.
            dat.Min = BakeModelSpace(ReadRawTriple(reader), dat.VertexScale);
            dat.Max = BakeModelSpace(ReadRawTriple(reader), dat.VertexScale);
        }

        if (dat.LodCount > 0 && lodArrayOffset != 0)
        {
            for (int lodIndex = 0; lodIndex < dat.LodCount; lodIndex++)
            {
                long lodPos = datBase + segmentBase + lodArrayOffset + lodIndex * 6L;
                if (lodPos + 6 > datBase + datSize)
                    break;

                reader.BaseStream.Seek(lodPos, SeekOrigin.Begin);
                var lod = new LodLevel();
                lod.Threshold = reader.ReadUInt16();
                lod.MeshCount = reader.ReadUInt16();
                var meshBaseOffset = reader.ReadUInt16();
                // Pool dedup is per-LOD: meshes with the same pVertexArray near-pointer
                // (very common — e.g. inn has 35 meshes referencing only 2 unique pools)
                // share a single LodLevel.VertexPools entry instead of duplicating data.
                var poolIndexByOffset = new Dictionary<ushort, int>();
                ReadMeshArray(reader, lod, datBase, segmentBase, datSize, meshBaseOffset, poolIndexByOffset);
                dat.Lods.Add(lod);
            }
        }

        // Bake every pool exactly once. Pools are deduped per LOD, so each shared pool is visited
        // a single time and cannot be double-scaled.
        foreach (var lod in dat.Lods)
            foreach (var pool in lod.VertexPools)
                foreach (var vertex in pool)
                    BakeModelSpace(vertex, dat.VertexScale);

        return dat;
    }

    /// <summary>
    /// Reads an array of mesh information from the provided binary data.
    /// </summary>
    /// <param name="reader">The binary reader positioned at the beginning of the data to read.</param>
    /// <param name="lod">The level of detail object containing mesh metadata.</param>
    /// <param name="datBase">The base address of the data block within the binary file.</param>
    /// <param name="segmentBase">The base offset of the current segment within the resource file.</param>
    /// <param name="datSize">The total size of the data block to be read.</param>
    /// <param name="meshBaseOffset"></param>
    /// <param name="poolIndexByOffset">Per-LOD dedup map: pVertexArray near-pointer → index
    /// into <see cref="LodLevel.VertexPools"/>.</param>
    private static void ReadMeshArray(BinaryReader reader, LodLevel lod, long datBase, uint segmentBase, long datSize,
        ushort meshBaseOffset, Dictionary<ushort, int> poolIndexByOffset)
    {
        if (lod.MeshCount == 0 || meshBaseOffset == 0)
            return;
        for (int meshIndex = 0; meshIndex < lod.MeshCount; meshIndex++)
        {
            long meshPos = datBase + segmentBase + meshBaseOffset + meshIndex * 14L;
            if (meshPos + 14 > datBase + datSize) break;
            lod.Meshes.Add(ParseMeshRecord(reader, meshPos, datBase, segmentBase, datSize, lod, poolIndexByOffset));
        }
    }

    /// <summary>
    /// Parse a single 14-byte mesh record + its vertex array, mesh-face array, and child mesh records.
    /// </summary>
    private static MeshRecord ParseMeshRecord(BinaryReader reader, long meshPos, long datBase, uint segmentBase,
        long datSize, LodLevel lod, Dictionary<ushort, int> poolIndexByOffset)
    {
        reader.BaseStream.Seek(meshPos, SeekOrigin.Begin);
        var mesh = new MeshRecord();
        mesh.RuntimeFlagsIndex = reader.ReadByte();
        mesh.SortNormalVertex = reader.ReadByte();
        mesh.SortAnchorVertex = reader.ReadByte();
        mesh.VertexCount = reader.ReadByte();
        var pVertexArray = reader.ReadUInt16();
        mesh.MeshFaceCount = reader.ReadUInt16();
        var pMeshFaceData = reader.ReadUInt16();
        mesh.ChildCount = reader.ReadUInt16();
        var pChildren = reader.ReadUInt16();

        if (pVertexArray != 0 && mesh.VertexCount > 0)
            mesh.VertexPoolIndex = ResolveVertexPool(reader, datBase, segmentBase, datSize, pVertexArray,
                mesh.VertexCount, lod, poolIndexByOffset);

        if (pMeshFaceData != 0 && mesh.MeshFaceCount > 0)
            ReadMeshFaces(reader, mesh, datBase, segmentBase, datSize, pMeshFaceData);

        if (pChildren != 0 && mesh.ChildCount > 0)
        {
            for (int childIndex = 0; childIndex < mesh.ChildCount; childIndex++)
            {
                long childPos = datBase + segmentBase + pChildren + childIndex * 14L;
                if (childPos + 14 > datBase + datSize) break;
                mesh.Children.Add(ParseMeshRecord(reader, childPos, datBase, segmentBase, datSize, lod, poolIndexByOffset));
            }
        }

        return mesh;
    }

    /// <summary>
    /// Return the index of the vertex pool at <paramref name="pVertexArray"/> in
    /// <paramref name="lod"/>, creating it on first sight. Returns -1 if the pool would
    /// extend past the DAT section's end.
    /// </summary>
    private static int ResolveVertexPool(BinaryReader reader, long datBase, uint segmentBase, long datSize,
        ushort pVertexArray, byte vertexCount, LodLevel lod, Dictionary<ushort, int> poolIndexByOffset)
    {
        if (poolIndexByOffset.TryGetValue(pVertexArray, out int existing))
        {
            // Empirically (all 12 shipped zones), meshes sharing a pVertexArray also share
            // VertexCount. If that ever breaks, dedup would silently truncate or over-read
            // a later mesh — surface it instead of guessing.
            int existingCount = lod.VertexPools[existing].Count;
            if (existingCount != vertexCount)
                throw new InvalidDataException(
                    $"Vertex pool dedup mismatch at pVertexArray=0x{pVertexArray:X4}: " +
                    $"existing count {existingCount} != new mesh count {vertexCount}");
            return existing;
        }

        long vertexPos = datBase + segmentBase + pVertexArray;
        if (vertexPos + vertexCount * 6L > datBase + datSize)
            return -1;

        reader.BaseStream.Seek(vertexPos, SeekOrigin.Begin);
        // Stored RAW here: the per-entity VertexScale isn't in scope this deep, and threading it
        // through ReadMeshArray/ParseMeshRecord would add an eighth parameter to an already
        // over-threaded chain. ParseEntity bakes every pool once, after the LOD walk.
        var pool = new List<Position3DInt>(vertexCount);
        for (int v = 0; v < vertexCount; v++)
        {
            pool.Add(ReadRawTriple(reader));
        }

        int newIndex = lod.VertexPools.Count;
        lod.VertexPools.Add(pool);
        poolIndexByOffset[pVertexArray] = newIndex;
        return newIndex;
    }

    private static void ReadMeshFaces(BinaryReader reader, MeshRecord mesh, long datBase, uint segmentBase,
        long datSize, ushort pMeshFaceData)
    {
        for (int meshFaceIndex = 0; meshFaceIndex < mesh.MeshFaceCount; meshFaceIndex++)
        {
            long recordPos = datBase + segmentBase + pMeshFaceData + meshFaceIndex * 8L;
            if (recordPos + 8 > datBase + datSize) break;

            reader.BaseStream.Seek(recordPos, SeekOrigin.Begin);
            byte renderType = reader.ReadByte();
            byte padding01 = reader.ReadByte();

            MeshFaceRecord record;
            if (renderType == 0)
            {
                var poly = new PolygonMeshFace
                {
                    RenderType = renderType,
                    Unknown01 = padding01,
                    FaceCount = reader.ReadUInt16()
                };
                var pFaceArray = reader.ReadUInt16();
                // Read +0x06 BEFORE walking the face array: ReadPolygonFaces seeks the shared
                // reader across the DAT section and does not restore position, so any field
                // read after it returns comes from wherever the walk stopped, not this record.
                poly.Unknown06 = reader.ReadUInt16();
                if (pFaceArray != 0 && poly.FaceCount > 0)
                    ReadPolygonFaces(reader, poly, datBase, segmentBase, datSize, pFaceArray);
                record = poly;
            }
            else if (renderType == 1)
            {
                record = new SpriteAMeshFace
                {
                    RenderType = renderType,
                    Unknown01 = padding01,
                    HalfSize = reader.ReadUInt16(),
                    VertexIndex = reader.ReadByte(),
                    ColorEga = reader.ReadByte(),
                    ColorVga = reader.ReadByte(),
                    Unknown07 = reader.ReadByte()
                };
            }
            else
            {
                record = new SpriteBMeshFace
                {
                    RenderType = renderType,
                    Unknown01 = padding01,
                    BitmapIndex = reader.ReadUInt16(),
                    AnchorY = reader.ReadByte(),
                    AnchorX = reader.ReadByte(),
                    SizeScale = reader.ReadByte(),
                    VertexIndex = reader.ReadByte()
                };
            }

            mesh.MeshFaces.Add(record);
        }
    }

    private static void ReadPolygonFaces(BinaryReader reader, PolygonMeshFace poly, long datBase, uint segmentBase,
        long datSize, ushort pFaceArray)
    {
        long datEnd = datBase + datSize;
        for (int faceIndex = 0; faceIndex < poly.FaceCount; faceIndex++)
        {
            long facePos = datBase + segmentBase + pFaceArray + faceIndex * 8L;
            if (facePos + 8 > datEnd)
                break;

            reader.BaseStream.Seek(facePos, SeekOrigin.Begin);
            var face = new PolygonFace
            {
                Flags = reader.ReadByte(),
                VgaColor = reader.ReadByte(),
                VgaShade = reader.ReadByte(),
                EgaColor = reader.ReadByte(),
                EgaShade = reader.ReadByte(),
                NormalVertexIndex = reader.ReadByte()
            };
            var pVertexIndexList = reader.ReadUInt16();

            if (pVertexIndexList != 0)
            {
                long listPos = datBase + segmentBase + pVertexIndexList;
                if (listPos < datEnd)
                {
                    reader.BaseStream.Seek(listPos, SeekOrigin.Begin);
                    while (listPos < datEnd)
                    {
                        byte idx = reader.ReadByte();
                        listPos++;
                        if (idx == 0xFF)
                        {
                            if (face.VertexIndices.Count < 3)
                                Console.WriteLine($"Reached end of vertex index list at face {faceIndex}, position {listPos} of {datEnd}");
                            break;
                        }
                        face.VertexIndices.Add(idx);
                    }
                }
            }

            poly.Faces.Add(face);
        }
    }
}
