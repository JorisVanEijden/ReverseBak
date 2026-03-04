namespace ResourceExtraction.Extractors;

using GameData.Resources.World;
using System.IO;
using System.Text;

public class ZoneShapeExtractor : ExtractorBase<ZoneShape>
{
    private const int ChapterCount = 9;
    public override ZoneShape Extract(string id, Stream resourceStream)
    {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var shape = new ZoneShape(id);
        for (int i = 0; i < ChapterCount; i++)
        {
            shape.Chapters.Add(new ChapterMonsters
            {
                Slot1 = (CreatureType)reader.ReadInt16(),
                Slot2 = (CreatureType)reader.ReadInt16(),
                Slot3 = (CreatureType)reader.ReadInt16(),
                Slot4 = (CreatureType)reader.ReadInt16()
            });
        }
        return shape;
    }
}
