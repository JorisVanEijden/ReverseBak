using System;

namespace ResourceExtraction.Extractors;

using GameData.Resources.World;
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

        return table;
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

    private static MapSection ParseMapSection(BinaryReader reader, (long offset, uint size) section)
    {
        reader.BaseStream.Seek(section.offset, SeekOrigin.Begin);
        var mapSection = new MapSection
        {
            Names = [],
            Unknown0 = reader.ReadUInt16()
        };

        ushort numItems = reader.ReadUInt16();
        var offsets = new ushort[numItems];
        for (int i = 0; i < numItems; i++)
            offsets[i] = reader.ReadUInt16();

        mapSection.UnknownTail = reader.ReadUInt16();
        long dataStart = reader.BaseStream.Position;

        for (int i = 0; i < numItems; i++)
        {
            reader.BaseStream.Seek(dataStart + offsets[i], SeekOrigin.Begin);
            mapSection.Names.Add(reader.ReadZeroTerminatedString());
        }

        return mapSection;
    }

    private static List<TableGidInfo> ParseGidSection(BinaryReader reader, (long offset, uint size) section,
        int numItems)
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
            var gid = new TableGidInfo
            {
                XRadius = reader.ReadUInt16(),
                YRadius = reader.ReadUInt16()
            };

            bool more = reader.ReadUInt16() > 0;
            gid.Flags = reader.ReadUInt16();

            if (more)
            {
                gid.Unknown06 = reader.ReadUInt16();
                byte subItemCount = reader.ReadByte();
                gid.Unknown09 = reader.ReadByte();
                gid.Unknown0A = reader.ReadUInt16();
                for (int j = 0; j < subItemCount; j++)
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
        dat.EntityType = reader.ReadByte();
        dat.TerrainType = reader.ReadByte();
        dat.VertexScale = reader.ReadByte();
        dat.Unknown04 = reader.ReadUInt16();
        dat.Unknown06 = reader.ReadUInt16();
        dat.LodCount = reader.ReadUInt16();
        var lodArrayOffset = reader.ReadUInt16();
        dat.Extent = reader.ReadInt16();

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
                ReadMeshArray(reader, lod, datBase, segmentBase, datSize, meshBaseOffset);
                dat.Lods.Add(lod);
            }
        }

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
    private static void ReadMeshArray(BinaryReader reader, LodLevel lod, long datBase, uint segmentBase, long datSize,
        ushort meshBaseOffset)
    {
        if (lod.MeshCount == 0 || meshBaseOffset == 0)
            return;
        for (int meshIndex = 0; meshIndex < lod.MeshCount; meshIndex++)
        {
            long meshPos = datBase + segmentBase + meshBaseOffset + meshIndex * 14L;
            if (meshPos + 14 > datBase + datSize) break;
            lod.Meshes.Add(ParseMeshRecord(reader, meshPos, datBase, segmentBase, datSize));
        }
    }

    /// <summary>
    /// Parse a single 14-byte mesh record + its vertex array, mesh-face array, and child mesh records.
    /// </summary>
    private static MeshRecord ParseMeshRecord(BinaryReader reader, long meshPos, long datBase, uint segmentBase,
        long datSize)
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
            ReadVertices(reader, mesh, datBase, segmentBase, datSize, pVertexArray);

        if (pMeshFaceData != 0 && mesh.MeshFaceCount > 0)
            ReadMeshFaces(reader, mesh, datBase, segmentBase, datSize, pMeshFaceData);

        if (pChildren != 0 && mesh.ChildCount > 0)
        {
            for (int childIndex = 0; childIndex < mesh.ChildCount; childIndex++)
            {
                long childPos = datBase + segmentBase + pChildren + childIndex * 14L;
                if (childPos + 14 > datBase + datSize) break;
                mesh.Children.Add(ParseMeshRecord(reader, childPos, datBase, segmentBase, datSize));
            }
        }

        return mesh;
    }

    private static void ReadVertices(BinaryReader reader, MeshRecord mesh, long datBase, uint segmentBase, long datSize,
        ushort pVertexArray)
    {
        long vertexPos = datBase + segmentBase + pVertexArray;
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
                if (pFaceArray != 0 && poly.FaceCount > 0)
                    ReadPolygonFaces(reader, poly, datBase, segmentBase, datSize, pFaceArray);
                poly.Unknown06 = reader.ReadUInt16();
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
                    Unknown04 = reader.ReadByte(),
                    Unknown05 = reader.ReadByte(),
                    Unknown06 = reader.ReadByte(),
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
