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
///        +0x00 u8  entityFlags       (bit 0x20 EF_UNBOUNDED → no bbox; bit 0x40 EF_2D_OBJECT → rotated cull)
///        +0x01 u8  entityType
///        +0x02 u8  terrainType
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
///        +0x01 u8  entityType
///        +0x02 u8  terrainType
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
    // entityFlags bit definitions (verified 2026-04-25 against IDA disasm):
    //   0x20 = EF_UNBOUNDED — when SET, the 12-byte bbox is omitted.
    //   0x40 = EF_2D_OBJECT — toggles rotated-cull path in sub_seg027_2BB.
    private const byte EF_UNBOUNDED = 0x20;

    // Defensive caps — guard against malformed records / wrong format guesses.
    private const int MaxLodLevels = 16;
    private const int MaxMeshesPerLod = 256;
    private const int MaxChildrenPerMesh = 256;
    private const int MaxRecursionDepth = 16;
    private const int MaxMeshFaceRecords = 256;
    private const int MaxPolygonFacesPerMesh = 256;
    private const int MaxVertexIndicesPerFace = 256;

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

        var datItems = ParseDatSection(reader, sections["DAT"], numItems, (long)sections["DAT"].size);

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

        ushort mapUnknown0 = reader.ReadUInt16();
        ushort numItems = reader.ReadUInt16();
        var offsets = new ushort[numItems];
        for (int i = 0; i < numItems; i++)
            offsets[i] = reader.ReadUInt16();

        ushort mapUnknownTail = reader.ReadUInt16();
        _ = mapUnknown0; _ = mapUnknownTail;
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
                ushort gidUnknown06 = reader.ReadUInt16();
                byte n = reader.ReadByte();
                byte gidUnknown09 = reader.ReadByte();
                ushort gidUnknown0A = reader.ReadUInt16();
                _ = gidUnknown06; _ = gidUnknown09; _ = gidUnknown0A;
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

    private static List<TableDatInfo> ParseDatSection(BinaryReader reader, (long offset, uint size) section, int numItems, long datSize)
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
    private static TableDatInfo ParseEntity(BinaryReader reader, long datBase, uint entityOffset, uint segmentBase, long datSize)
    {
        reader.BaseStream.Seek(datBase + entityOffset, SeekOrigin.Begin);
        var dat = new TableDatInfo
        {
            EntityFlags = reader.ReadByte(),
            EntityType = reader.ReadByte(),
            TerrainType = reader.ReadByte(),
            VertexScale = reader.ReadByte(),
            Unknown04 = reader.ReadUInt16(),
            Unknown06 = reader.ReadUInt16(),
            LodCount = reader.ReadUInt16(),
            LodArrayOffset = reader.ReadUInt16(),
            Extent = reader.ReadInt16()
        };

        // Bbox follows the 14-byte header iff EF_UNBOUNDED is clear. Reader is positioned at +0x0E.
        if ((dat.EntityFlags & EF_UNBOUNDED) == 0)
        {
            dat.Min = new Position3DShort
            {
                X = reader.ReadInt16(),
                Y = reader.ReadInt16(),
                Z = reader.ReadInt16()
            };
            dat.Max = new Position3DShort
            {
                X = reader.ReadInt16(),
                Y = reader.ReadInt16(),
                Z = reader.ReadInt16()
            };
        }

        if (dat.LodCount > 0 && dat.LodCount <= MaxLodLevels && dat.LodArrayOffset != 0)
        {
            for (int li = 0; li < dat.LodCount; li++)
            {
                long lodPos = datBase + segmentBase + dat.LodArrayOffset + li * 6L;
                if (lodPos + 6 > datBase + datSize) break;

                reader.BaseStream.Seek(lodPos, SeekOrigin.Begin);
                var lod = new LodLevel
                {
                    Threshold = reader.ReadUInt16(),
                    MeshCount = reader.ReadUInt16(),
                    MeshBaseOffset = reader.ReadUInt16()
                };
                ReadMeshArray(reader, lod, datBase, segmentBase, datSize);
                dat.Lods.Add(lod);
            }
        }

        return dat;
    }

    private static void ReadMeshArray(BinaryReader reader, LodLevel lod, long datBase, uint segmentBase, long datSize)
    {
        if (lod.MeshCount == 0 || lod.MeshBaseOffset == 0) return;
        int count = lod.MeshCount > MaxMeshesPerLod ? MaxMeshesPerLod : lod.MeshCount;
        for (int m = 0; m < count; m++)
        {
            long meshPos = datBase + segmentBase + lod.MeshBaseOffset + m * 14L;
            if (meshPos + 14 > datBase + datSize) break;
            lod.Meshes.Add(ParseMeshRecord(reader, meshPos, datBase, segmentBase, datSize, depth: 0));
        }
    }

    /// <summary>
    /// Parse a single 14-byte mesh record + its vertex array, mesh-face array, and child mesh records.
    /// </summary>
    private static MeshRecord ParseMeshRecord(BinaryReader reader, long meshPos, long datBase, uint segmentBase, long datSize, int depth)
    {
        reader.BaseStream.Seek(meshPos, SeekOrigin.Begin);
        var mesh = new MeshRecord
        {
            RuntimeFlagsIndex = reader.ReadByte(),
            EntityType = reader.ReadByte(),
            TerrainType = reader.ReadByte(),
            VertexCount = reader.ReadByte(),
            PVertexArray = reader.ReadUInt16(),
            MeshFaceCount = reader.ReadUInt16(),
            PMeshFaceData = reader.ReadUInt16(),
            ChildCount = reader.ReadUInt16(),
            PChildren = reader.ReadUInt16()
        };

        if (mesh.PVertexArray != 0 && mesh.VertexCount > 0)
            ReadVertices(reader, mesh, datBase, segmentBase, datSize);

        if (mesh.PMeshFaceData != 0 && mesh.MeshFaceCount > 0)
            ReadMeshFaces(reader, mesh, datBase, segmentBase, datSize);

        if (mesh.PChildren != 0 && mesh.ChildCount > 0
            && mesh.ChildCount <= MaxChildrenPerMesh && depth < MaxRecursionDepth)
        {
            for (int c = 0; c < mesh.ChildCount; c++)
            {
                long childPos = datBase + segmentBase + mesh.PChildren + c * 14L;
                if (childPos + 14 > datBase + datSize) break;
                mesh.Children.Add(ParseMeshRecord(reader, childPos, datBase, segmentBase, datSize, depth + 1));
            }
        }

        return mesh;
    }

    private static void ReadVertices(BinaryReader reader, MeshRecord mesh, long datBase, uint segmentBase, long datSize)
    {
        long vertexPos = datBase + segmentBase + mesh.PVertexArray;
        if (vertexPos + mesh.VertexCount * 6L > datBase + datSize) return;

        reader.BaseStream.Seek(vertexPos, SeekOrigin.Begin);
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            mesh.Vertices.Add(new Position3DShort
            {
                X = reader.ReadInt16(),
                Y = reader.ReadInt16(),
                Z = reader.ReadInt16()
            });
        }
    }

    private static void ReadMeshFaces(BinaryReader reader, MeshRecord mesh, long datBase, uint segmentBase, long datSize)
    {
        int count = mesh.MeshFaceCount > MaxMeshFaceRecords ? MaxMeshFaceRecords : mesh.MeshFaceCount;
        for (int m = 0; m < count; m++)
        {
            long recordPos = datBase + segmentBase + mesh.PMeshFaceData + m * 8L;
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
                    Padding01 = padding01,
                    FaceCount = reader.ReadUInt16(),
                    PFaceArray = reader.ReadUInt16(),
                    Reserved06 = reader.ReadUInt16()
                };
                if (poly.PFaceArray != 0 && poly.FaceCount > 0)
                    ReadPolygonFaces(reader, poly, datBase, segmentBase, datSize);
                record = poly;
            }
            else if (renderType == 1)
            {
                record = new SpriteAMeshFace
                {
                    RenderType = renderType,
                    Padding01 = padding01,
                    HalfSize = reader.ReadUInt16(),
                    VertexIndex = reader.ReadByte(),
                    ColorEga = reader.ReadByte(),
                    ColorVga = reader.ReadByte(),
                    Reserved07 = reader.ReadByte()
                };
            }
            else
            {
                record = new SpriteBMeshFace
                {
                    RenderType = renderType,
                    Padding01 = padding01,
                    BitmapIndex = reader.ReadUInt16(),
                    Reserved04 = reader.ReadByte(),
                    Reserved05 = reader.ReadByte(),
                    Reserved06 = reader.ReadByte(),
                    VertexIndex = reader.ReadByte()
                };
            }

            mesh.MeshFaces.Add(record);
        }
    }

    private static void ReadPolygonFaces(BinaryReader reader, PolygonMeshFace poly, long datBase, uint segmentBase, long datSize)
    {
        long datEnd = datBase + datSize;
        int count = poly.FaceCount > MaxPolygonFacesPerMesh ? MaxPolygonFacesPerMesh : poly.FaceCount;
        for (int f = 0; f < count; f++)
        {
            long facePos = datBase + segmentBase + poly.PFaceArray + f * 8L;
            if (facePos + 8 > datEnd) break;

            reader.BaseStream.Seek(facePos, SeekOrigin.Begin);
            var face = new PolygonFace
            {
                Flags = reader.ReadByte(),
                VgaColor = reader.ReadByte(),
                VgaShade = reader.ReadByte(),
                EgaColor = reader.ReadByte(),
                EgaShade = reader.ReadByte(),
                NormalVertexIndex = reader.ReadByte(),
                PVertexIndexList = reader.ReadUInt16()
            };

            if (face.PVertexIndexList != 0)
            {
                long listPos = datBase + segmentBase + face.PVertexIndexList;
                if (listPos < datEnd)
                {
                    reader.BaseStream.Seek(listPos, SeekOrigin.Begin);
                    int safety = 0;
                    while (listPos < datEnd && safety < MaxVertexIndicesPerFace)
                    {
                        byte idx = reader.ReadByte();
                        listPos++;
                        safety++;
                        if (idx == 0xFF) break;
                        face.VertexIndices.Add(idx);
                    }
                }
            }

            poly.Faces.Add(face);
        }
    }
}
