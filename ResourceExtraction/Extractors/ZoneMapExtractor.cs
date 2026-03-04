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
        zoneMap.BitmapData = reader.ReadBytes(ZoneMap.TotalBytes);
        return zoneMap;
    }
}
