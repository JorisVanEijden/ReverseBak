namespace ResourceExtraction.Extractors;

using GameData.Resources.Content;
using GameData.Resources.World;
using System.IO;
using System.Text;

public class WorldItemExtractor : ExtractorBase<WorldTile>
{
    private const int BytesPerItem = 20;
    private const int MaxItems = 300;

    /// <summary>Side of one world tile in game units — the square a Tzzxxyy file describes.</summary>
    private const long TileSize = 64000;

    /// <summary>
    /// How far outside its own tile a record may still be real. Objects legitimately overhang a
    /// tile border: across all 351 shipped tiles, 70 records sit outside their square and the
    /// furthest is 32,001 units — half a tile, i.e. something anchored right on the edge. The
    /// corrupt records this guards against are millions of units out, so a full tile of slack
    /// separates the two by roughly 30x in either direction.
    /// </summary>
    private const long MaxOverhang = TileSize;

    public override WorldTile Extract(string id, Stream resourceStream)
    {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var tile = new WorldTile(id);

        string name = Path.GetFileNameWithoutExtension(id);
        if (name.Length >= 7 && (name[0] == 'T' || name[0] == 't'))
        {
            if (byte.TryParse(name.Substring(1, 2), out byte zone)) tile.ZoneNumber = zone;
            if (byte.TryParse(name.Substring(3, 2), out byte x)) tile.X = x;
            if (byte.TryParse(name.Substring(5, 2), out byte y)) tile.Y = y;
        }

        int itemCount = (int)(resourceStream.Length / BytesPerItem);
        if (itemCount > MaxItems) itemCount = MaxItems;

        for (int i = 0; i < itemCount; i++)
        {
            ushort typeId = reader.ReadUInt16();
            var item = new WorldItem
            {
                TypeId = typeId,
                EntityKey = ContentKey.ForBase($"tbl:z{tile.ZoneNumber:D2}", typeId),
                Rotation = new Rotation3D
                {
                    X = reader.ReadUInt16(),
                    Y = reader.ReadUInt16(),
                    Z = reader.ReadUInt16()
                },
                Position = new Position3D
                {
                    X = reader.ReadUInt32(),
                    Y = reader.ReadUInt32(),
                    Z = reader.ReadUInt32()
                }
            };

            // T091011.WLD ships with 8 corrupt records (indices 248-255): 160 bytes of
            // high-entropy junk spliced into the middle of the file, with valid data resuming at
            // record 256 still on the 20-byte grid. It is the only affected file of 351.
            //
            // The 1993 engine reads them too — czone_load_actors uses this same
            // filelength/0x14 count — and gets away with it because ts_get_shape returns NULL for
            // their out-of-range ids and the caller dereferences it without checking
            // (CZONE.C:138, SHAPETBL.C:153). Whatever kind byte that read produces, the entries end
            // up millions of units from the tile and are never drawn.
            //
            // We drop them instead of passing junk to consumers. The test is position, not id:
            // record 248 carries a perfectly valid shape id (141) and only its coordinates give it
            // away.
            if (IsPlaceable(item, tile))
            {
                tile.Items.Add(item);
            }
            else
            {
                tile.DiscardedItems++;
            }
        }
        return tile;
    }

    /// <summary>
    /// Could this record describe a real object on this tile? True unless its position lies more
    /// than <see cref="MaxOverhang"/> outside the tile's own square. Tiles whose name did not parse
    /// (zone/x/y all zero) are left alone rather than guessed at.
    /// </summary>
    private static bool IsPlaceable(WorldItem item, WorldTile tile)
    {
        if (tile.X == 0 && tile.Y == 0 && tile.ZoneNumber == 0)
        {
            return true;
        }
        long left = (long)tile.X * TileSize - MaxOverhang;
        long top = (long)tile.Y * TileSize - MaxOverhang;
        long right = ((long)tile.X + 1) * TileSize + MaxOverhang;
        long bottom = ((long)tile.Y + 1) * TileSize + MaxOverhang;
        return item.Position.X >= left && item.Position.X < right
            && item.Position.Y >= top && item.Position.Y < bottom;
    }
}
