namespace ResourceExtraction.Extractors;

using GameData.Resources.World;
using ResourceExtraction.Extensions;
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
///   DAT: rendering data — geometry, sprites, bounding boxes
///
/// DAT section layout:
///   Offset table: numItems × 4 bytes (u16 pair → combined offset within section)
///   Entity data blocks at each offset:
///     [14 bytes] Entity header (flags, type, terrain, vertexScale, lodCount, etc.)
///     [12 bytes] Bounding box (if bounded: entityFlags &amp; 0x20 == 0)
///     [6 × lodCount] LOD records (distanceThreshold, meshCount, meshBaseOffset)
///     [14 × meshCount] Mesh records (vertices, faces, children)
///     Vertex arrays, face group records, face records, vertex index lists
///
/// All sub-structure offsets are segment-relative within the game's data segment,
/// converted to file positions via: filePos = dataStart + (segOffset - regionBase)
///
/// Verified against IDA disassembly: RenderWorldItem (0x2a89f), renderShapeDispatcher
/// (0x2a70c), processEntityFaces (0x23a48), LoadZoneTableDatTag (0x31503).
/// </summary>
public class ZoneTableExtractor : ExtractorBase<ZoneTable>
{
    private const byte EF_UNBOUNDED = 0x20;
    private const byte EF_2D_OBJECT = 0x40;

    public override ZoneTable Extract(string id, Stream resourceStream)
    {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var table = new ZoneTable(id);

        var sections = ParseSectionHeaders(reader);

        if (!sections.ContainsKey("MAP") || !sections.ContainsKey("DAT"))
            throw new InvalidDataException($"TBL file {id} missing required MAP or DAT sections");

        var names = ParseMapSection(reader, sections["MAP"]);
        int numItems = names.Count;

        var gidItems = sections.ContainsKey("GID")
            ? ParseGidSection(reader, sections["GID"], numItems)
            : new List<TableGidInfo>();

        var datItems = ParseDatSection(reader, sections["DAT"], numItems);

        for (int i = 0; i < numItems; i++)
        {
            table.Entries.Add(new ZoneTableEntry
            {
                Index = i,
                Name = names[i],
                Dat = i < datItems.Count ? datItems[i] : new TableDatInfo(),
                Gid = i < gidItems.Count ? gidItems[i] : new TableGidInfo()
            });
        }

        return table;
    }

    private static Dictionary<string, (long offset, uint size)> ParseSectionHeaders(BinaryReader reader)
    {
        var sections = new Dictionary<string, (long offset, uint size)>();
        while (reader.BaseStream.Position < reader.BaseStream.Length - 8)
        {
            string tag = reader.ReadTag();
            uint size = reader.ReadUInt32();
            if (string.IsNullOrEmpty(tag) || size == 0)
                break;
            sections[tag] = (reader.BaseStream.Position, size);
            reader.BaseStream.Seek(size, SeekOrigin.Current);
        }
        return sections;
    }

    private static List<string> ParseMapSection(BinaryReader reader, (long offset, uint size) section)
    {
        reader.BaseStream.Seek(section.offset, SeekOrigin.Begin);
        var names = new List<string>();

        reader.ReadUInt16(); // skip first u16
        ushort numItems = reader.ReadUInt16();
        var offsets = new ushort[numItems];
        for (int i = 0; i < numItems; i++)
            offsets[i] = reader.ReadUInt16();

        reader.ReadUInt16(); // skip trailing u16
        long dataStart = reader.BaseStream.Position;

        for (int i = 0; i < numItems; i++)
        {
            reader.BaseStream.Seek(dataStart + offsets[i], SeekOrigin.Begin);
            names.Add(reader.ReadZeroTerminatedString());
        }

        return names;
    }

    private static List<TableGidInfo> ParseGidSection(BinaryReader reader, (long offset, uint size) section, int numItems)
    {
        reader.BaseStream.Seek(section.offset, SeekOrigin.Begin);
        var items = new List<TableGidInfo>();

        var offsets = new uint[numItems];
        for (int i = 0; i < numItems; i++)
        {
            ushort lower = reader.ReadUInt16();
            ushort upper = reader.ReadUInt16();
            offsets[i] = ((uint)upper << 4) + (uint)(lower & 0x000f);
        }

        for (int i = 0; i < numItems; i++)
        {
            reader.BaseStream.Seek(section.offset + offsets[i], SeekOrigin.Begin);
            var gid = new TableGidInfo();

            gid.XRadius = reader.ReadUInt16();
            gid.YRadius = reader.ReadUInt16();
            bool more = reader.ReadUInt16() > 0;
            gid.Flags = reader.ReadUInt16();

            if (more)
            {
                reader.BaseStream.Seek(2, SeekOrigin.Current);
                byte n = reader.ReadByte();
                reader.BaseStream.Seek(3, SeekOrigin.Current);
                for (int j = 0; j < n; j++)
                {
                    int u = reader.ReadSByte();
                    int v = reader.ReadSByte();
                    int x = reader.ReadInt16();
                    int y = reader.ReadInt16();
                    gid.TextureCoords.Add(new TableGidCoord { X = u, Y = v });
                    gid.OtherCoords.Add(new TableGidCoord { X = x, Y = y });
                }
            }

            items.Add(gid);
        }

        return items;
    }

    /// <summary>
    /// Parse DAT section — rendering data with geometry and sprite references.
    ///
    /// Per-entity layout (verified against IDA: RenderWorldItem 0x2a89f,
    /// sub_seg027_5D9 0x2a6d9, LoadZoneTableDatTag 0x31503):
    ///
    ///   Entity header (14 bytes):
    ///     +0x00 byte  entityFlags      (0x20=unbounded, 0x40=2D object)
    ///     +0x01 byte  entityType
    ///     +0x02 byte  terrainType
    ///     +0x03 byte  vertexScale      (vertex coords left-shifted by this amount)
    ///     +0x04 word  (reserved)
    ///     +0x06 word  (reserved)
    ///     +0x08 word  lodCount         (0 = sprite/empty, &gt;0 = number of LOD levels)
    ///     +0x0A word  lodArrayOffset   (segment-relative offset to LOD array — game uses for far ptr)
    ///     +0x0C word  extent           (draw distance, shifted by vertexScale)
    ///
    ///   If bounded (entityFlags &amp; 0x20 == 0):
    ///     +0x0E  6×int16  bounding box (minX, minY, minZ, maxX, maxY, maxZ)
    ///
    ///   LOD records (6 bytes each × lodCount):
    ///     +0x00 word  distanceThreshold  (game selects LOD where viewDist &lt;= threshold)
    ///     +0x02 word  meshCount          (number of 14-byte mesh records at this LOD)
    ///     +0x04 word  meshBaseOffset     (segment-relative offset to mesh records)
    ///
    ///   Mesh records (14 bytes each, contiguous from meshBaseOffset):
    ///     +0x00 3 bytes (skip — runtime flags index, etc.)
    ///     +0x03 byte  vertexCount
    ///     +0x04 word  vertexOffset     (segment-relative)
    ///     +0x06 word  faceGroupCount
    ///     +0x08 word  faceGroupOffset  (segment-relative)
    ///     +0x0A word  childCount
    ///     +0x0C word  childOffset      (segment-relative)
    ///
    ///   All sub-structure offsets are segment-relative (absolute within the game's
    ///   data segment). Converted to file positions via:
    ///     filePos = dataStart + (segOffset - regionBase)
    ///   where dataStart is the file position after all LOD records,
    ///   and regionBase = meshBaseOffset of the first LOD record.
    /// </summary>
    private static List<TableDatInfo> ParseDatSection(BinaryReader reader, (long offset, uint size) section, int numItems)
    {
        reader.BaseStream.Seek(section.offset, SeekOrigin.Begin);
        var items = new List<TableDatInfo>();

        // Offset table: each entry is a pair of u16, combined into an absolute offset within the section
        var offsets = new uint[numItems];
        for (int i = 0; i < numItems; i++)
        {
            ushort lower = reader.ReadUInt16();
            ushort upper = reader.ReadUInt16();
            offsets[i] = ((uint)upper << 4) + (uint)(lower & 0x000f);
        }

        for (int i = 0; i < numItems; i++)
        {
            var dat = ParseDatEntity(reader, section.offset + offsets[i], section.offset + section.size, i);
            items.Add(dat);
        }

        return items;
    }

    private static TableDatInfo ParseDatEntity(BinaryReader reader, long itemBase, long sectionEnd, int index)
    {
        reader.BaseStream.Seek(itemBase, SeekOrigin.Begin);
        var dat = new TableDatInfo();

        // --- Entity header (14 bytes) ---
        dat.EntityFlags = reader.ReadByte();
        dat.EntityType = reader.ReadByte();
        dat.TerrainType = reader.ReadByte();
        dat.VertexScale = reader.ReadByte();
        reader.BaseStream.Seek(4, SeekOrigin.Current); // skip reserved fields (+4..+7)
        ushort lodCount = reader.ReadUInt16();          // +8: number of LOD levels (0 = no geometry)
        reader.BaseStream.Seek(4, SeekOrigin.Current);  // skip +A (lodArrayOffset) and +C (extent)

        if (lodCount == 0)
        {
            // No geometry — check for direct sprite reference
            TryReadDirectSprite(reader, dat);
            return dat;
        }

        // --- Bounding box (12 bytes, bounded entities only) ---
        bool bounded = (dat.EntityFlags & EF_UNBOUNDED) == 0;
        if (bounded)
        {
            dat.Min = new Position3DShort
            {
                X = reader.ReadInt16(), Y = reader.ReadInt16(), Z = reader.ReadInt16()
            };
            dat.Max = new Position3DShort
            {
                X = reader.ReadInt16(), Y = reader.ReadInt16(), Z = reader.ReadInt16()
            };
        }

        // --- LOD records (6 bytes each × lodCount) ---
        // Each LOD: distanceThreshold(u16), meshCount(u16), meshBaseOffset(u16)
        // Game selects LOD where viewDistance <= threshold; falls back to last LOD.
        var lodRecords = new List<(ushort threshold, ushort meshCount, ushort meshBaseOffset)>();
        for (int j = 0; j < lodCount; j++)
        {
            ushort threshold = reader.ReadUInt16();
            ushort meshCount = reader.ReadUInt16();
            ushort meshBaseOffset = reader.ReadUInt16();
            lodRecords.Add((threshold, meshCount, meshBaseOffset));
        }

        // dataStart = file position AFTER all LOD records (where mesh data region begins)
        // regionBase = meshBaseOffset of first LOD (corresponds to dataStart in segment space)
        long dataStart = itemBase + 14 + (bounded ? 12 : 0) + lodCount * 6;
        ushort regionBase = lodRecords[0].meshBaseOffset;

        // --- Read mesh records from all LOD levels ---
        // Use all LODs to capture maximum geometry. Different LODs may reference
        // different mesh groups at different offsets within the same data region.
        var vertexSets = new List<(uint count, uint offset)>();
        var meshRecords = new List<MeshRecord>();
        uint vertexAccum = 0;

        foreach (var (_, meshCount, meshBaseOffset) in lodRecords)
        {
            SeekToOffset(reader, dataStart, meshBaseOffset, regionBase);

            for (int m = 0; m < meshCount; m++)
            {
                reader.BaseStream.Seek(3, SeekOrigin.Current); // skip (runtime flags index, etc.)
                byte vertexCount = reader.ReadByte();           // +3
                ushort vertexOffset = reader.ReadUInt16();       // +4
                ushort faceGroupCount = reader.ReadUInt16();     // +6
                ushort faceGroupOffset = reader.ReadUInt16();    // +8
                reader.BaseStream.Seek(4, SeekOrigin.Current);  // skip (child count, child offset)

                // Track unique vertex sets: different meshes may share the same vertex buffer
                var key = ((uint)vertexCount, (uint)vertexOffset);
                if (vertexSets.Count == 0 || vertexSets[^1] != key)
                {
                    if (vertexSets.Count > 0)
                        vertexAccum += vertexSets[^1].count;
                    vertexSets.Add(key);
                }

                meshRecords.Add(new MeshRecord
                {
                    VertexIndexBase = vertexAccum,
                    FaceGroupCount = faceGroupCount,
                    FaceGroupOffset = faceGroupOffset
                });
            }
        }

        // --- Total vertex count ---
        uint totalVertices = vertexAccum;
        if (vertexSets.Count > 0)
            totalVertices += vertexSets[^1].count;

        // Standard entity types store an extra origin vertex at index 0 (not referenced by faces).
        // We still read it, but the -1 gate prevents treating single-vertex entities as geometry.
        int effectiveVertices = (int)totalVertices;
        if (IsStandardEntityType(dat.EntityType) && effectiveVertices > 0)
            effectiveVertices -= 1;

        if (effectiveVertices <= 0)
        {
            // No renderable geometry — might be a sprite entity with LOD structure
            TryReadDirectSprite(reader, dat);
            return dat;
        }

        // --- Read vertex data from unique vertex sets ---
        ReadVertices(reader, dat, vertexSets, dataStart, regionBase);

        // --- Read face data from all mesh records ---
        // Verified against IDA (RenderWorldItem 0x2a89f, renderShapeDispatcher 0x2a70c):
        ReadFaces(reader, dat, meshRecords, meshRecords.Count, dataStart, regionBase);

        return dat;
    }

    private static void ReadVertices(BinaryReader reader, TableDatInfo dat,
        List<(uint count, uint offset)> vertexSets, long dataStart, ushort regionBase)
    {
        var readOffsets = new HashSet<uint>();
        foreach (var (count, offset) in vertexSets)
        {
            if (!readOffsets.Add(offset))
                continue; // already read this vertex buffer

            SeekToOffset(reader, dataStart, offset, regionBase);
            for (int v = 0; v < count; v++)
            {
                dat.Vertices.Add(new Position3DShort
                {
                    X = reader.ReadInt16(),
                    Y = reader.ReadInt16(),
                    Z = reader.ReadInt16()
                });
            }
        }
    }

    private static void ReadFaces(BinaryReader reader, TableDatInfo dat,
        List<MeshRecord> meshRecords, int groupLimit, long dataStart, ushort regionBase)
    {
        for (int j = 0; j < meshRecords.Count && j < groupLimit; j++)
        {
            var mesh = meshRecords[j];

            // Read face group headers (8 bytes each)
            SeekToOffset(reader, dataStart, mesh.FaceGroupOffset, regionBase);
            var faceGroups = new List<FaceGroupHeader>();
            for (int fg = 0; fg < mesh.FaceGroupCount; fg++)
            {
                var group = new FaceGroupHeader
                {
                    Type = reader.ReadUInt16(),
                    FaceCount = reader.ReadUInt16(),
                    FaceArrayOffset = reader.ReadUInt16()
                };
                reader.BaseStream.Seek(2, SeekOrigin.Current); // skip extra data
                faceGroups.Add(group);

                // Face type 2 = sprite reference (FaceCount is the sprite index)
                if (group.Type == 2)
                    dat.Sprite = group.FaceCount;
            }

            // Read individual faces from each polygon face group
            foreach (var group in faceGroups)
            {
                if (group.Type == 2)
                    continue; // sprite group, not polygon faces

                ReadFaceGroup(reader, dat, group, mesh.VertexIndexBase, dataStart, regionBase);
            }
        }
    }

    private static void ReadFaceGroup(BinaryReader reader, TableDatInfo dat,
        FaceGroupHeader group, uint vertexIndexBase, long dataStart, ushort regionBase)
    {
        // Face records are 8 bytes each, but we navigate via offsets rather than sequential reads
        // because face records contain a vertexListOffset that requires seeking.
        uint currentOffset = group.FaceArrayOffset;

        for (int f = 0; f < group.FaceCount; f++)
        {
            SeekToOffset(reader, dataStart, currentOffset, regionBase);

            var face = new TableFace
            {
                PaletteSource = reader.ReadByte(),  // +0
                FaceColor = reader.ReadByte(),       // +1
                EdgeColor = reader.ReadByte(),       // +2
                Color3 = reader.ReadByte(),          // +3
                Color4 = reader.ReadByte()           // +4
            };
            reader.BaseStream.Seek(1, SeekOrigin.Current); // +5: skip normal vertex index
            ushort vertexListOffset = reader.ReadUInt16();  // +6

            // Record current position as the next face's offset
            currentOffset = (uint)(reader.BaseStream.Position - dataStart + regionBase);

            // Read 0xFF-terminated vertex index list
            SeekToOffset(reader, dataStart, vertexListOffset, regionBase);
            var indices = new List<int>();
            byte idx;
            while ((idx = reader.ReadByte()) != 0xFF)
            {
                indices.Add(idx + (int)vertexIndexBase);
            }

            face.VertexIndices = indices;
            dat.Faces.Add(face);
        }
    }

    /// <summary>
    /// Try to read a direct sprite reference for entities without polygon geometry.
    /// Sprite entities have flags: unbounded (0x20) + 2D object (0x40).
    /// After the entity header, the sprite index follows a 2-byte skip.
    /// </summary>
    private static void TryReadDirectSprite(BinaryReader reader, TableDatInfo dat)
    {
        if ((dat.EntityFlags & EF_UNBOUNDED) != 0
            && (dat.EntityFlags & EF_2D_OBJECT) != 0
            && reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
        {
            reader.BaseStream.Seek(2, SeekOrigin.Current);
            dat.Sprite = reader.ReadUInt16();
            reader.BaseStream.Seek(4, SeekOrigin.Current);
        }
    }

    /// <summary>
    /// Seek to a position within the entity's data region using a segment-relative offset.
    /// filePos = dataStart + (segOffset - regionBase)
    /// </summary>
    private static void SeekToOffset(BinaryReader reader, long dataStart, uint segOffset, ushort regionBase)
    {
        reader.BaseStream.Seek(dataStart + segOffset - regionBase, SeekOrigin.Begin);
    }

    /// <summary>
    /// Entity types where totalVertices includes an extra origin vertex at index 0
    /// that is not referenced by face data. From BaKGL analysis (mScale adjustment).
    /// </summary>
    private static bool IsStandardEntityType(byte entityType)
    {
        return entityType is 0x0 or 0x1 or 0x2 or 0x3 or 0x4 or 0x5
            or 0x6 or 0x7 or 0x8 or 0x9 or 0xa
            or 0xe or 0xf or 0x12 or 0x14 or 0x17
            or 0x24 or 0x26 or 0x27;
    }

    /// <summary>Mesh record within a mesh group — tracks vertex and face offsets.</summary>
    private class MeshRecord
    {
        public uint VertexIndexBase { get; set; } // Cumulative offset for multi-mesh vertex index remapping
        public ushort FaceGroupCount { get; set; }
        public ushort FaceGroupOffset { get; set; }
    }

    /// <summary>Face group header — groups faces by rendering type.</summary>
    private class FaceGroupHeader
    {
        public ushort Type { get; set; }            // 0=polygon, 2=sprite
        public ushort FaceCount { get; set; }       // Number of faces (or sprite index if Type==2)
        public ushort FaceArrayOffset { get; set; } // Offset to face records in DAT buffer
    }
}
