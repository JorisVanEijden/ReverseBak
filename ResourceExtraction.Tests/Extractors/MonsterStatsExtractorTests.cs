namespace ResourceExtraction.Tests.Extractors;

using ResourceExtraction.Extractors;
using Xunit;

public class MonsterStatsExtractorTests
{
    [Fact]
    public void Extract_ReadsMaxBeforeMin()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)50);  // Health min
        writer.Write((ushort)100); // Health max
        writer.Write((ushort)40);  // Stamina min
        writer.Write((ushort)80);  // Stamina max
        for (int i = 0; i < 10; i++)
        {
            writer.Write((ushort)0);
            writer.Write((ushort)0);
        }
        writer.Flush();
        stream.Position = 0;

        var extractor = new MonsterStatsExtractor();
        var result = extractor.Extract("MONST18.DAT", stream);

        Assert.Equal(18, result.CreatureId);
        Assert.Equal((ushort)100, result.Health.Max);
        Assert.Equal((ushort)50, result.Health.Min);
        Assert.Equal((ushort)80, result.Stamina.Max);
        Assert.Equal((ushort)40, result.Stamina.Min);
    }

    [Fact]
    public void Extract_ParsesCreatureIdFromFilename()
    {
        using var stream = new MemoryStream(new byte[48]);
        var extractor = new MonsterStatsExtractor();
        var result = extractor.Extract("MONST52.DAT", stream);
        Assert.Equal(52, result.CreatureId);
    }
}
