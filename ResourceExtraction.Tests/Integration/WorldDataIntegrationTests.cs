namespace ResourceExtraction.Tests.Integration;

using GameData.Resources.Monster;
using GameData.Resources.World;
using ResourceExtraction.Extractors;
using Xunit;

public class WorldDataIntegrationTests
{
    private const string GamePath = "/home/revai/bak/OriginalGame";

    private static bool GameFilesExist => Directory.Exists(GamePath);

    [SkippableFact]
    public void ZoneDef_AllZonesParseWithoutErrors()
    {
        Skip.IfNot(GameFilesExist, "Game files not found");
        var extractor = new ZoneDefExtractor();
        foreach (string defFile in Directory.GetFiles(GamePath, "Z??DEF.DAT"))
        {
            using var stream = File.OpenRead(defFile);
            var def = extractor.Extract(Path.GetFileName(defFile), stream);
            Assert.True(def.ZoneLocation >= 0);
        }
    }

    [SkippableFact]
    public void ZoneMap_AllZonesParseAs400Bytes()
    {
        Skip.IfNot(GameFilesExist, "Game files not found");
        var extractor = new ZoneMapExtractor();
        foreach (string mapFile in Directory.GetFiles(GamePath, "Z??MAP.DAT"))
        {
            using var stream = File.OpenRead(mapFile);
            var map = extractor.Extract(Path.GetFileName(mapFile), stream);
            Assert.Equal(400, map.BitmapData.Length);
        }
    }

    [SkippableFact]
    public void WorldTile_ParsesWithoutErrors()
    {
        Skip.IfNot(GameFilesExist, "Game files not found");
        var extractor = new WorldItemExtractor();
        string[] wldFiles = Directory.GetFiles(GamePath, "T??????.WLD");
        Assert.NotEmpty(wldFiles);
        foreach (string wldFile in wldFiles)
        {
            using var stream = File.OpenRead(wldFile);
            var tile = extractor.Extract(Path.GetFileName(wldFile), stream);
            Assert.True(tile.Items.Count <= 300);
            Assert.True(tile.ZoneNumber > 0);
        }
    }

    [SkippableFact]
    public void MonsterStats_AllMonstersParseWithoutErrors()
    {
        Skip.IfNot(GameFilesExist, "Game files not found");
        var extractor = new MonsterStatsExtractor();
        string[] monstFiles = Directory.GetFiles(GamePath, "MONST*.DAT");
        Assert.NotEmpty(monstFiles);
        foreach (string monstFile in monstFiles)
        {
            using var stream = File.OpenRead(monstFile);
            var stats = extractor.Extract(Path.GetFileName(monstFile), stream);
            Assert.True(stats.CreatureId > 0);
        }
    }
}
