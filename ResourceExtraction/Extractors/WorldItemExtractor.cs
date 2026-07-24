namespace ResourceExtraction.Extractors;

using GameData.Resources.Content;
using GameData.Resources.World;
using System.IO;
using System.Text;

public class WorldItemExtractor : ExtractorBase<WorldTile>
{
    private const int BytesPerItem = 20;
    private const int MaxItems = 300;

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
            tile.Items.Add(new WorldItem
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
            });
        }
        return tile;
    }
}
