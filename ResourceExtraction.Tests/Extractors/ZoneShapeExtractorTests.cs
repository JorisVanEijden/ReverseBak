namespace ResourceExtraction.Tests.Extractors;

using GameData.Resources.World;
using ResourceExtraction.Extractors;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

/// <summary>
/// <c>Z##SHP.DAT</c> — the four creature types a zone offers per chapter. Every slot is cast
/// straight to <see cref="CreatureType"/>, so an id the enum does not cover becomes a bogus value
/// with no complaint; these tests are what would notice.
/// </summary>
public class ZoneShapeExtractorTests {
    /// <summary>Walk up from the test output dir to find OriginalGame/&lt;name&gt; (present on dev
    /// machines, absent on CI). Returns null when the shipped data isn't available.</summary>
    private static string? FindGameFile(string name) {
        string? dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            string candidate = Path.Combine(dir, "OriginalGame", name);
            if (File.Exists(candidate)) {
                return candidate;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    private static IEnumerable<CreatureType> SlotsOf(ChapterMonsters row) {
        yield return row.Slot1;
        yield return row.Slot2;
        yield return row.Slot3;
        yield return row.Slot4;
    }

    [Fact]
    public void Extract_Reads9ChaptersWith4Slots() {
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

    [Fact]
    public void AChapterRowIsFourSlotsAndTheRowsAreInChapterOrder() {
        using var stream = new MemoryStream(BuildFile());

        ZoneShape zone = new ZoneShapeExtractor().Extract("Z01SHP.DAT", stream);

        Assert.Equal(ZoneMonsterRoster.ChapterCount, zone.Chapters.Count);
        Assert.Equal(CreatureType.Troll, zone.Chapters[0].Slot1);
        Assert.Equal(CreatureType.Rogue, zone.Chapters[1].Slot1);
    }

    [Fact]
    public void MinusOneIsTheEmptySlotSentinel() {
        using var stream = new MemoryStream(BuildFile());

        ZoneShape zone = new ZoneShapeExtractor().Extract("Z01SHP.DAT", stream);

        Assert.Equal(CreatureType.None, zone.Chapters[0].Slot4);
    }

    // Chapter 1 = Troll, Rogue, Rogue, empty; chapter 2 = Rogue in slot 1; the rest empty.
    private static byte[] BuildFile() {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((short)CreatureType.Troll);
        w.Write((short)CreatureType.Rogue);
        w.Write((short)CreatureType.Rogue);
        w.Write((short)-1);
        w.Write((short)CreatureType.Rogue);
        for (var i = 0; i < (ZoneMonsterRoster.ChapterCount * ZoneMonsterRoster.SlotsPerChapter) - 5; i++) {
            w.Write((short)-1);
        }

        return ms.ToArray();
    }

    [SkippableTheory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    public void EverySlotInTheShippedFilesIsACreatureTypeWeActuallyKnow(int zoneNumber) {
        // The cast is unchecked, so an unmapped id would flow into the game as a creature that does
        // not exist rather than as an error.
        string name = $"Z{zoneNumber:D2}SHP.DAT";
        string? path = FindGameFile(name);
        Skip.If(path == null, $"OriginalGame/{name} not found");
        using FileStream stream = File.OpenRead(path!);

        ZoneShape zone = new ZoneShapeExtractor().Extract(name, stream);

        Assert.Equal(ZoneMonsterRoster.ChapterCount, zone.Chapters.Count);
        foreach (ChapterMonsters row in zone.Chapters) {
            foreach (CreatureType slot in SlotsOf(row)) {
                Assert.True(Enum.IsDefined(typeof(CreatureType), slot),
                    $"{name} carries creature id {(int)slot}, which CreatureType does not cover");
            }
        }
    }
}
