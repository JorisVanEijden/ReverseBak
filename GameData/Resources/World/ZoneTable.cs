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
/// </summary>
public class TableDatInfo
{
    public byte EntityFlags { get; set; }
    public byte EntityType { get; set; }
    public byte TerrainType { get; set; }
    public byte VertexScale { get; set; }

    /// <summary>Sprite index into concatenated SLOT BMX array. -1 if polygon geometry.</summary>
    public int Sprite { get; set; } = -1;

    /// <summary>Bounding box minimum (only for bounded entities).</summary>
    public Position3DShort? Min { get; set; }
    /// <summary>Bounding box maximum (only for bounded entities).</summary>
    public Position3DShort? Max { get; set; }

    /// <summary>3D vertices for polygon entities.</summary>
    public List<Position3DShort> Vertices { get; set; } = new();

    /// <summary>Face definitions: each face is a list of vertex indices.</summary>
    public List<TableFace> Faces { get; set; } = new();
}

/// <summary>
/// A polygon face with color, palette source, and vertex indices.
/// </summary>
public class TableFace
{
    public byte PaletteSource { get; set; }
    public byte FaceColor { get; set; }
    public byte EdgeColor { get; set; }
    public byte Color3 { get; set; }
    public byte Color4 { get; set; }
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
