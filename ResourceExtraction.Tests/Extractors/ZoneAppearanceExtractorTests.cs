namespace ResourceExtraction.Tests.Extractors;

using GameData.Resources.World;
using ResourceExtraction.Extractors;
using System.IO;
using Xunit;

/// <summary>
/// <c>Z##.DAT</c> — the sky/ground pens and the overhead map's pen remap.
/// </summary>
public class ZoneAppearanceExtractorTests {
    [Fact]
    public void UnlistedPensAreLeftALONE() {
        // The original fills the table with identity and then overwrites; a reader that returns
        // zeroes for the pens the file does not mention would paint the whole map in pen 0.
        ZoneAppearance zone = Read(sky: 215, ground: 231, unused: 0, (0, 230), (3, 214));

        Assert.Equal(230, zone.MapPenFor(0));
        Assert.Equal(214, zone.MapPenFor(3));
        Assert.Equal(1, zone.MapPenFor(1));
        Assert.Equal(255, zone.MapPenFor(255));
        Assert.Equal(2, zone.RemappedPenCount);

        // And the same through the 256-entry table the renderer wants.
        byte[] table = zone.ToPenTable();
        Assert.Equal(256, table.Length);
        Assert.Equal(230, table[0]);
        Assert.Equal(1, table[1]);
        Assert.Equal(255, table[255]);
    }

    [Fact]
    public void APairsListThatIsEmptyStillGivesAUsableTable() {
        // What the underground zones ship: eight bytes, all zero. They never reach this render, but
        // a table of zeroes would be a trap for whatever reads it next.
        ZoneAppearance zone = Read(sky: 0, ground: 0, unused: 0);

        Assert.Equal(0, zone.RemappedPenCount);
        Assert.Equal(77, zone.MapPenFor(77));
    }

    [Fact]
    public void TheThreeHeaderValuesAreReadInTheOrderTheOriginalReadsThem() {
        ZoneAppearance zone = Read(sky: 168, ground: 230, unused: 5);

        Assert.Equal(168, zone.SkyPen);
        Assert.Equal(230, zone.GroundPen);
        Assert.Equal(5, zone.UnusedPen);
    }

    [Theory]
    [InlineData("Z01.DAT", 215, 231, 6)]
    [InlineData("Z08.DAT", 168, 230, 6)]
    [InlineData("Z09.DAT", 215, 193, 6)]
    // Zone 6 is the odd one out: it remaps a whole run of high pens as well as the usual six.
    [InlineData("Z06.DAT", 215, 231, 23)]
    // Underground: an empty file, because the overhead map draws the automap there instead.
    [InlineData("Z10.DAT", 0, 0, 0)]
    [InlineData("Z12.DAT", 0, 0, 0)]
    public void TheShippedZonesReadAsExpected(string name, int sky, int ground, int remapped) {
        string path = Find(name);
        if (path == null) {
            return;
        }

        using FileStream stream = File.OpenRead(path);
        ZoneAppearance zone = new ZoneAppearanceExtractor().Extract(name, stream);

        Assert.Equal(sky, zone.SkyPen);
        Assert.Equal(ground, zone.GroundPen);
        Assert.Equal(remapped, zone.RemappedPenCount);
        // Never used, zero everywhere — pinned so a non-zero one would be noticed rather than
        // silently ignored.
        Assert.Equal(0, zone.UnusedPen);
    }

    [Fact]
    public void EveryShippedZoneFileIsAccountedForToTheBYTE() {
        string path = Find("Z01.DAT");
        if (path == null) {
            return;
        }

        for (var i = 1; i <= 12; i++) {
            string name = $"Z{i:D2}.DAT";
            string file = Find(name);
            Assert.NotNull(file);
            using FileStream stream = File.OpenRead(file);
            long length = stream.Length;
            ZoneAppearance zone = new ZoneAppearanceExtractor().Extract(name, stream);

            // Header plus two bytes per pair — nothing before it, nothing after it. Checked against
            // the file length rather than the stream position, which BinaryReader runs ahead of.
            Assert.Equal(8 + (2 * zone.Remaps.Length), length);
        }
    }

    private static ZoneAppearance Read(int sky, int ground, int unused, params (byte Pen, byte To)[] pairs) {
        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, System.Text.Encoding.ASCII, leaveOpen: true)) {
            writer.Write((ushort)sky);
            writer.Write((ushort)ground);
            writer.Write((ushort)unused);
            writer.Write((ushort)pairs.Length);
            foreach ((byte pen, byte to) in pairs) {
                writer.Write(pen);
                writer.Write(to);
            }
        }

        buffer.Position = 0;
        return new ZoneAppearanceExtractor().Extract("TEST.DAT", buffer);
    }

    private static string Find(string name) {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null) {
            string candidate = Path.Combine(dir.FullName, "OriginalGame", name);
            if (File.Exists(candidate)) {
                return candidate;
            }
            dir = dir.Parent;
        }

        return null;
    }
}
