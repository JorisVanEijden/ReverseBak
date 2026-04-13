namespace ResourceExtraction.Extractors;

using GameData.Resources.World;
using ResourceExtraction.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Extracts Z##.TBL files — tagged resource files containing world item type definitions.
/// Format has 4 tag sections: MAP (name strings), APP (skipped), GID (grid/collision), DAT (rendering).
/// Based on xBaK/BaKGL reverse engineering, verified against IDA disassembly.
/// </summary>
public class ZoneTableExtractor : ExtractorBase<ZoneTable>
{
    private const byte EF_UNBOUNDED = 0x20;
    private const byte EF_2D_OBJECT = 0x40;

    public override ZoneTable Extract(string id, Stream resourceStream)
    {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var table = new ZoneTable(id);

        // Parse tagged sections
        var sections = new Dictionary<string, (long offset, uint size)>();
        while (reader.BaseStream.Position < reader.BaseStream.Length - 8)
        {
            long tagStart = reader.BaseStream.Position;
            string tag = reader.ReadTag();
            uint size = reader.ReadUInt32();

            if (string.IsNullOrEmpty(tag) || size == 0)
                break;

            sections[tag] = (reader.BaseStream.Position, size);
            reader.BaseStream.Seek(size, SeekOrigin.Current);
        }

        if (!sections.ContainsKey("MAP") || !sections.ContainsKey("DAT"))
            throw new InvalidDataException($"TBL file {id} missing required MAP or DAT sections");

        // Parse MAP section — name string table
        var names = ParseMapSection(reader, sections["MAP"]);
        int numItems = names.Count;

        // Parse GID section — grid/collision info
        var gidItems = sections.ContainsKey("GID")
            ? ParseGidSection(reader, sections["GID"], numItems)
            : new List<TableGidInfo>();

        // Parse DAT section — rendering data
        var datItems = ParseDatSection(reader, sections["DAT"], numItems);

        // Combine into entries
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

        // Read offset table: each entry is 2x u16, combined as (upper << 4) + (lower & 0x000f)
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
                reader.BaseStream.Seek(2, SeekOrigin.Current); // skip 2
                byte n = reader.ReadByte();
                reader.BaseStream.Seek(3, SeekOrigin.Current); // skip 1 + 2
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

    private static List<TableDatInfo> ParseDatSection(BinaryReader reader, (long offset, uint size) section, int numItems)
    {
        reader.BaseStream.Seek(section.offset, SeekOrigin.Begin);
        var items = new List<TableDatInfo>();

        // Read offset table
        var offsets = new uint[numItems];
        for (int i = 0; i < numItems; i++)
        {
            ushort lower = reader.ReadUInt16();
            ushort upper = reader.ReadUInt16();
            offsets[i] = ((uint)upper << 4) + (uint)(lower & 0x000f);
        }

        for (int i = 0; i < numItems; i++)
        {
            try
            {
            long itemBaseOffset = section.offset + offsets[i];
            reader.BaseStream.Seek(itemBaseOffset, SeekOrigin.Begin);
            var dat = new TableDatInfo();

            dat.EntityFlags = reader.ReadByte();
            dat.EntityType = reader.ReadByte();
            dat.TerrainType = reader.ReadByte();
            dat.VertexScale = reader.ReadByte();
            reader.BaseStream.Seek(4, SeekOrigin.Current); // skip 4
            bool more = reader.ReadUInt16() > 0;
            reader.BaseStream.Seek(4, SeekOrigin.Current); // skip 4

            // Calculate data offset like BaKGL: offset += 14 + (bounded ? 12 : 0)
            long dataOffset = itemBaseOffset + 14;
            
            Console.WriteLine($"[TBL {i}] itemBaseOffset={itemBaseOffset}, dataOffset={dataOffset}, section=[{section.offset},{section.offset+section.size}], streamLen={reader.BaseStream.Length}");

            if (more)
            {
                // Bounding box for bounded entities
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
                    dataOffset += 12; // Add bounding box size
                }

                reader.BaseStream.Seek(2, SeekOrigin.Current); // skip 2
                ushort nComponents = reader.ReadUInt16();
                ushort baseOffset = reader.ReadUInt16(); // base offset for relative addressing

                // First pass: read component headers and track unique vertex sets
                // BaKGL uses (vertexCount, vertexOffset) pairs to identify unique vertex buffers
                var vertexSets = new List<(uint count, uint offset)>(); // unique vertex sets
                var vertexSums = new List<uint>(); // cumulative sum of vertex counts per set
                var meshOffsetDatas = new List<MeshOffsetData>(); // per-mesh data

                // First pass: read component headers (skip 2, meshCount, meshOffset)
                var componentDatas = new List<(ushort meshCount, ushort meshOffset)>();
                for (int j = 0; j < nComponents; j++)
                {
                    reader.BaseStream.Seek(2, SeekOrigin.Current); // skip 2 bytes
                    ushort meshCount = reader.ReadUInt16();
                    ushort meshOffset = reader.ReadUInt16();
                    componentDatas.Add((meshCount, meshOffset));
                }

                // Calculate data offset (position after entity header)
                long dataOffset = itemBaseOffset + 14 + ((dat.EntityFlags & EF_UNBOUNDED) == 0 ? 12 : 0);

                // Second pass: seek to each component's mesh data using meshOffset
                uint currentVertexSum = 0;
                foreach (var (meshCount, meshOffset) in componentDatas)
                {
                    reader.BaseStream.Seek(dataOffset + meshOffset - baseOffset, SeekOrigin.Begin);

                    for (int meshI = 0; meshI < meshCount; meshI++)
                    {
                        reader.BaseStream.Seek(3, SeekOrigin.Current); // skip 3 bytes
                        byte vertexCount = reader.ReadByte();
                        ushort vertexOffset = reader.ReadUInt16();
                        ushort faceCount = reader.ReadUInt16();
                        ushort faceOffset = reader.ReadUInt16();
                        reader.BaseStream.Seek(4, SeekOrigin.Current); // skip 4 bytes

                        if (i == 0 && meshI == 0)
                        {
                            Console.WriteLine($"[TBL {i}] Mesh: v={vertexCount},vOff={vertexOffset},f={faceCount},fOff={faceOffset}");
                        }

                        // Check if this is a new unique vertex set
                        var vertexSetKey = ((uint)vertexCount, (uint)vertexOffset);
                        if (vertexSets.Count == 0 || vertexSets[^1] != vertexSetKey)
                        {
                            if (vertexSets.Count > 0)
                            {
                                currentVertexSum += vertexSets[^1].count;
                            }
                            vertexSets.Add(vertexSetKey);
                            vertexSums.Add(currentVertexSum);
                        }
                        else
                        {
                            vertexSums.Add(currentVertexSum);
                        }

                        meshOffsetDatas.Add(new MeshOffsetData
                        {
                            VertexCount = vertexCount,
                            VertexOffset = vertexOffset,
                            VertexIndexTransform = vertexSums[^1],
                            FaceCount = faceCount,
                            FaceOffset = faceOffset
                        });
                    }
                }

                // Calculate total vertex count
                uint totalVertices = currentVertexSum;
                if (vertexSets.Count > 0)
                    totalVertices += vertexSets[^1].count;

                // Adjust vertex count based on entity type (per BaKGL)
                int nVertices = (int)totalVertices;
                if (IsStandardEntityType(dat.EntityType) && nVertices > 0)
                    nVertices -= 1;

                if (nVertices > 0)
                {
                    // Read all vertices from all unique vertex sets
                    var uniqueOffsets = new HashSet<uint>();
                    foreach (var set in vertexSets)
                    {
                        uint offset = set.offset;
                        if (!uniqueOffsets.Add(offset))
                            continue; // already read this offset

                        long vertexSeekPos = dataOffset + offset - baseOffset;
                        if (i == 0)
                        {
                            Console.WriteLine($"[TBL {i}] Reading vertices: offset={offset}, baseOffset={baseOffset}, seekPos={vertexSeekPos}, streamLen={reader.BaseStream.Length}");
                        }
                        reader.BaseStream.Seek(vertexSeekPos, SeekOrigin.Begin);
                        for (int v = 0; v < set.count; v++)
                        {
                            if (i == 0 && v == 0)
                            {
                                Console.WriteLine($"[TBL {i}] Before ReadInt16: streamPos={reader.BaseStream.Position}, streamLen={reader.BaseStream.Length}");
                            }
                            dat.Vertices.Add(new Position3DShort
                            {
                                X = reader.ReadInt16(),
                                Y = reader.ReadInt16(),
                                Z = reader.ReadInt16()
                            });
                        }
                    }

                    // Read face definitions per component - matching BaKGL approach
                    int adjustedComponents = dat.EntityType == 0x0a ? nComponents - 1 : nComponents;
                    for (int j = 0; j < adjustedComponents && j < meshOffsetDatas.Count; j++)
                    {
                        var meshData = meshOffsetDatas[j];
                        uint vertexIndexTransform = meshData.VertexIndexTransform;

                        // Seek to this mesh's face data using its faceOffset
                        long faceDataOffset = dataOffset + meshData.FaceOffset - baseOffset;
                        if (i == 0 && j == 0)
                        {
                            Console.WriteLine($"[TBL {i}] Seeking to face data: dataOffset={dataOffset}, faceOffset={meshData.FaceOffset}, baseOffset={baseOffset}, calc={faceDataOffset}");
                            // Also check what's at other positions
                            reader.BaseStream.Seek(21556, SeekOrigin.Begin);
                            byte[] checkBytes = reader.ReadBytes(16);
                            Console.WriteLine($"[TBL {i}] Hex dump at 21556: {BitConverter.ToString(checkBytes)}");
                            reader.BaseStream.Seek(21568, SeekOrigin.Begin);
                            checkBytes = reader.ReadBytes(16);
                            Console.WriteLine($"[TBL {i}] Hex dump at 21568: {BitConverter.ToString(checkBytes)}");
                        }
                        reader.BaseStream.Seek(faceDataOffset, SeekOrigin.Begin);

                        // Read face headers (like FaceData in BaKGL)
                        var faceHeaders = new List<FaceHeader>();
                        if (i == 0 && j == 0)
                        {
                            // Hex dump first 16 bytes at face data position
                            long pos = reader.BaseStream.Position;
                            byte[] bytes = reader.ReadBytes(16);
                            Console.WriteLine($"[TBL {i}] Hex dump at face data pos {pos}: {BitConverter.ToString(bytes)}");
                            reader.BaseStream.Seek(pos, SeekOrigin.Begin); // reset
                        }
                        for (int f = 0; f < meshData.FaceCount; f++)
                        {
                            if (i == 0 && f == 0)
                            {
                                Console.WriteLine($"[TBL {i}] Reading face header {f} at streamPos={reader.BaseStream.Position}");
                            }
                            ushort faceType = reader.ReadUInt16();
                            ushort edgeCount = reader.ReadUInt16();
                            ushort edgeOffset = reader.ReadUInt16();
                            reader.BaseStream.Seek(2, SeekOrigin.Current); // skip 2
                            if (i == 0 && f == 0)
                            {
                                Console.WriteLine($"[TBL {i}] Face {f}: faceType={faceType}, edgeCount={edgeCount}, edgeOffset={edgeOffset}");
                            }

                            faceHeaders.Add(new FaceHeader
                            {
                                FaceType = faceType,
                                EdgeCount = edgeCount,
                                EdgeOffset = edgeOffset
                            });

                            // Face type 2 indicates sprite
                            if (faceType == 2)
                            {
                                dat.Sprite = edgeCount;
                            }
                        }

                        // Read face data - seek to each face's edgeOffset for vertex indices
                        for (int f = 0; f < meshData.FaceCount; f++)
                        {
                            var header = faceHeaders[f];
                            if (header.FaceType == 2)
                                continue; // skip sprite faces

                            // Seek to this face's edge data
                            long edgeDataOffset = dataOffset + header.EdgeOffset - baseOffset;
                            reader.BaseStream.Seek(edgeDataOffset, SeekOrigin.Begin);

                            // Read edges - each edge becomes a face with vertex indices
                            // BaKGL pattern: track edgeSeekOffset, read header, seek to vertices, return
                            uint edgeSeekOffset = header.EdgeOffset;
                            for (int e = 0; e < header.EdgeCount; e++)
                            {
                                // Seek to this edge's data
                                long edgeSeekPos = dataOffset + edgeSeekOffset - baseOffset;
                                if (i == 25)
                                {
                                    Console.WriteLine($"[TBL {i}] Face {f} Edge {e}: edgeSeekOffset={edgeSeekOffset}, seeking to {edgeSeekPos}, streamLength={reader.BaseStream.Length}");
                                }
                                reader.BaseStream.Seek(edgeSeekPos, SeekOrigin.Begin);

                                var face = new TableFace();
                                face.PaletteSource = reader.ReadByte();
                                face.FaceColor = reader.ReadByte();
                                face.EdgeColor = reader.ReadByte();
                                face.Color3 = reader.ReadByte();
                                face.Color4 = reader.ReadByte();
                                reader.BaseStream.Seek(1, SeekOrigin.Current); // skip group
                                ushort vertexListOffset = reader.ReadUInt16();

                                // Update edgeSeekOffset for next edge (current position after reading header)
                                edgeSeekOffset = (uint)(reader.BaseStream.Position - dataOffset + baseOffset);

                                // Now seek to the vertex list and read indices
                                long vertexListPos = dataOffset + vertexListOffset - baseOffset;
                                reader.BaseStream.Seek(vertexListPos, SeekOrigin.Begin);

                                var indices = new List<int>();
                                byte vertIdx;
                                while ((vertIdx = reader.ReadByte()) != 0xFF)
                                {
                                    // Apply cumulative vertex index transform
                                    indices.Add(vertIdx + (int)vertexIndexTransform);
                                }

                                face.VertexIndices = indices;
                                dat.Faces.Add(face);
                            }
                        }
                    }
                }
                else
                {
                    // Sprite entity
                    if ((dat.EntityFlags & EF_UNBOUNDED) != 0
                        && (dat.EntityFlags & EF_2D_OBJECT) != 0
                        && nComponents == 1)
                    {
                        reader.BaseStream.Seek(2, SeekOrigin.Current);
                        dat.Sprite = reader.ReadUInt16();
                        reader.BaseStream.Seek(4, SeekOrigin.Current);
                    }
                }
            }

            items.Add(dat);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to parse TBL item {i} at offset {offsets[i]}: {ex.Message}");
                throw;
            }
        }

        return items;
    }

    /// <summary>
    /// Entity types that require nVertices -= 1 adjustment (from BaKGL analysis).
    /// </summary>
    private static bool IsStandardEntityType(byte entityType)
    {
        return entityType is 0x0 or 0x1 or 0x2 or 0x3 or 0x4 or 0x5
            or 0x6 or 0x7 or 0x8 or 0x9 or 0xa
            or 0xe or 0xf or 0x12 or 0x14 or 0x17
            or 0x24 or 0x26 or 0x27;
    }

    /// <summary>
    /// Per-mesh data for tracking vertex and face offsets during TBL parsing.
    /// </summary>
    private class MeshOffsetData
    {
        public uint VertexCount { get; set; }
        public uint VertexOffset { get; set; }
        public uint VertexIndexTransform { get; set; } // Cumulative offset to apply to indices
        public ushort FaceCount { get; set; }
        public ushort FaceOffset { get; set; }
    }

    /// <summary>
    /// Face header data from TBL file.
    /// </summary>
    private class FaceHeader
    {
        public ushort FaceType { get; set; }
        public ushort EdgeCount { get; set; }
        public ushort EdgeOffset { get; set; }
    }
}
