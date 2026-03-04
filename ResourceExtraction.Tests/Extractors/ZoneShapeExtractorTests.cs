namespace ResourceExtraction.Tests.Extractors;

using GameData.Resources.World;
using ResourceExtraction.Extractors;
using Xunit;

public class ZoneShapeExtractorTests
{
    [Fact]
    public void Extract_Reads9ChaptersWith4Slots()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((short)15); writer.Write((short)16);
        writer.Write((short)-1); writer.Write((short)-1);
        for (int i = 1; i < 9; i++)
            for (int j = 0; j < 4; j++)
                writer.Write((short)-1);
        writer.Flush();
        stream.Position = 0;

        var extractor = new ZoneShapeExtractor();
        var result = extractor.Extract("Z01SHP.DAT", stream);
        Assert.Equal(9, result.Chapters.Count);
        Assert.Equal(CreatureType.Gorath, result.Chapters[0].Slot1);
        Assert.Equal(CreatureType.Owyn, result.Chapters[0].Slot2);
        Assert.Equal(CreatureType.None, result.Chapters[0].Slot3);
        Assert.Equal(CreatureType.None, result.Chapters[1].Slot1);
    }
}
