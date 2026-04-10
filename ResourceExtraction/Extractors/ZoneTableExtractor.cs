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
            reader.BaseStream.Seek(section.offset + offsets[i], SeekOrigin.Begin);
            var dat = new TableDatInfo();

            dat.EntityFlags = reader.ReadByte();
            dat.EntityType = reader.ReadByte();
            dat.TerrainType = reader.ReadByte();
            dat.VertexScale = reader.ReadByte();
            reader.BaseStream.Seek(4, SeekOrigin.Current); // skip 4
            bool more = reader.ReadUInt16() > 0;
            reader.BaseStream.Seek(4, SeekOrigin.Current); // skip 4

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
                }

                reader.BaseStream.Seek(2, SeekOrigin.Current); // skip 2
                ushort nComponents = reader.ReadUInt16();
                reader.BaseStream.Seek(2, SeekOrigin.Current); // skip 2 (seems important per BaKGL)

                // Read component headers to determine vertex count
                int nVertices = 0;
                int prevV = 0;
                int changeOverComponent = 0;
                int changeOverVerticesOffset = 0;
                for (int j = 0; j < nComponents; j++)
                {
                    reader.BaseStream.Seek(3, SeekOrigin.Current);
                    byte v = reader.ReadByte();
                    if (v != prevV)
                    {
                        nVertices += v;
                        changeOverComponent = j;
                        changeOverVerticesOffset = prevV;
                        prevV = v;
                    }
                    reader.BaseStream.Seek(10, SeekOrigin.Current);
                }

                // Adjust vertex count based on entity type (per BaKGL)
                if (IsStandardEntityType(dat.EntityType) && nVertices > 0)
                    nVertices -= 1;

                if (nVertices > 0)
                {
                    // Polygon entity — read vertices
                    for (int j = 0; j <= nVertices; j++)
                    {
                        dat.Vertices.Add(new Position3DShort
                        {
                            X = reader.ReadInt16(),
                            Y = reader.ReadInt16(),
                            Z = reader.ReadInt16()
                        });
                    }

                    // Read face definitions per component
                    int adjustedComponents = dat.EntityType == 0x0a ? nComponents - 1 : nComponents;
                    for (int j = 0; j < adjustedComponents; j++)
                    {
                        // Read face count entries (terminated by non-zero first u16)
                        var faceCounts = new List<ushort>();
                        while (reader.BaseStream.Position < section.offset + section.size - 1)
                        {
                            ushort marker = reader.ReadUInt16();
                            if (marker != 0)
                            {
                                reader.BaseStream.Seek(-2, SeekOrigin.Current);
                                break;
                            }
                            faceCounts.Add(reader.ReadUInt16());
                            reader.BaseStream.Seek(4, SeekOrigin.Current); // skip offset
                        }

                        if (faceCounts.Count == 0)
                            continue;

                        foreach (ushort faceCount in faceCounts)
                        {
                            // Read face colors/palettes
                            var pendingFaces = new List<TableFace>();
                            for (int k = 0; k < faceCount; k++)
                            {
                                var face = new TableFace();
                                face.PaletteSource = reader.ReadByte();
                                face.FaceColor = reader.ReadByte();
                                face.EdgeColor = reader.ReadByte();
                                face.Color3 = reader.ReadByte();
                                face.Color4 = reader.ReadByte();
                                reader.BaseStream.Seek(3, SeekOrigin.Current); // skip offset
                                pendingFaces.Add(face);
                            }

                            // Read face vertex indices (0xFF terminated per face)
                            for (int k = 0; k < faceCount; k++)
                            {
                                var indices = new List<int>();
                                while (reader.BaseStream.Position < section.offset + section.size)
                                {
                                    byte vertIdx = reader.ReadByte();
                                    if (vertIdx == 0xFF)
                                        break;
                                    // Apply changeover offset for later components
                                    if (j >= changeOverComponent)
                                        vertIdx = (byte)(vertIdx + changeOverVerticesOffset);
                                    indices.Add(vertIdx);
                                }
                                pendingFaces[k].VertexIndices = indices;
                                dat.Faces.Add(pendingFaces[k]);
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
}
