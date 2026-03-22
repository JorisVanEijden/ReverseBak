namespace ResourceExtraction.Extractors;

using GameData.Resources.World;
using System.IO;
using System.Text;

public class ZoneMapExtractor : ExtractorBase<ZoneMap>
{
    public override ZoneMap Extract(string id, Stream resourceStream)
    {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var zoneMap = new ZoneMap(id);
        var rawBytes = reader.ReadBytes(ZoneMap.Height * 8);
        for (int y = 0; y < ZoneMap.Height; y++)
        {
            for (int x = 0; x < ZoneMap.Width; x++)
            {
                int byteIndex = y * 8 + x / 8;
                int bit = x % 8;
                zoneMap.Tiles[x][y] = ((rawBytes[byteIndex] >> bit) & 1) == 1;
            }
        }
        return zoneMap;
    }
}
