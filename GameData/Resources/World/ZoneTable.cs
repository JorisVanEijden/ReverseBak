namespace GameData.Resources.World;

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

/// <summary>
/// DAT section entry — rendering properties and geometry for a world item type.
/// Entity header is 14 bytes: flags, type, terrain, vertexScale, field_4, field_6,
/// lodCount, lodArrayOffset, extent. Verified against IDA (entityDatRecord + RenderWorldItem 0x2a89f).
/// </summary>
public class TableDatInfo
{
    public byte EntityFlags { get; set; }
    public byte EntityType { get; set; }
    public byte TerrainType { get; set; }
    public byte VertexScale { get; set; }

    /// <summary>Entity header +4 (u16). Always 0 in samples — likely runtime-only. TBD.</summary>
    public ushort Unknown04 { get; set; }
    /// <summary>Entity header +6 (u16). Always 0 in samples — likely runtime-only. TBD.</summary>
    public ushort Unknown06 { get; set; }
    /// <summary>Entity header +C (i16). Draw-distance/extent, shifted by VertexScale.</summary>
    public short Extent { get; set; }

    /// <summary>Sprite index into concatenated SLOT BMX array. -1 if polygon geometry.</summary>
    public int Sprite { get; set; } = -1;

    /// <summary>Bounding box minimum (only for bounded entities).</summary>
    public Position3DShort? Min { get; set; }
    /// <summary>Bounding box maximum (only for bounded entities).</summary>
    public Position3DShort? Max { get; set; }

    /// <summary>3D vertices for polygon entities.</summary>
    public List<Position3DShort> Vertices { get; set; } = new();

    /// <summary>Face definitions — each face is an 8-byte record plus a vertex index list.</summary>
    public List<TableFace> Faces { get; set; } = new();
}

/// <summary>
/// An 8-byte polygon face record + its vertex index list.
/// Verified against IDA: processEntityFaces (0x23a48), calculateFaceShading (0x248f4),
/// backfaceCullDotProduct (0x239d4).
///
/// Layout:
///   +0x00 u8  Flags              bits 0-1 = type (0=two-sided, 1/3=single-sided, 2=sprite skip),
///                                bit 4 (0x10) = underground shadow highlight,
///                                bit 5 (0x20) = enable shading math (else flat color),
///                                bit 7 (0x80) = force flat "drawColor 1", skip shading.
///   +0x01 u8  VgaColor           palette index used as pen color when VGA_byte_dseg_20DF == 0
///   +0x02 u8  VgaShade           palette index + shade-table input (VGA mode)
///   +0x03 u8  EgaColor           palette index used when VGA_byte_dseg_20DF != 0 (EGA/CGA)
///   +0x04 u8  EgaShade           palette index + shade-table input (EGA/CGA mode)
///   +0x05 u8  NormalVertexIndex  vertex index used for face-normal direction (backface cull)
///   +0x06 u16 pVertexIndexList   pointer to 0xFF-terminated vertex-index list (winding order)
///
/// Face bytes are palette indices, but the engine's per-zone Z##L.SCX "material strip"
/// system can turn selected pen colors into samples from a loaded strip image at draw
/// time. sub_seg022_71 populates color_to_texture_mapping[256] from 27 textureStripConfig
/// slots in seg086; the rasterizer (sub_seg022_21E) then substitutes a strip sample for
/// any pen color whose mapping entry is not 0xFFFF. LOD and zone gates on
/// textureStripConfig.flags decide which pen colors are active at any given detail level
/// — no per-entity/per-face flag is involved.
/// </summary>
public class TableFace
{
    public byte Flags { get; set; }
    public byte VgaColor { get; set; }
    public byte VgaShade { get; set; }
    public byte EgaColor { get; set; }
    public byte EgaShade { get; set; }
    public byte NormalVertexIndex { get; set; }
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
}

public class TableGidCoord
{
    public int X { get; set; }
    public int Y { get; set; }
}

/// <summary>
/// Short (16-bit signed) 3D position, used for TBL vertices and bounding boxes.
/// </summary>
public class Position3DShort
{
    public short X { get; set; }
    public short Y { get; set; }
    public short Z { get; set; }
}
