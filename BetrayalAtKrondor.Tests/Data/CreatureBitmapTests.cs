namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Creature;
using GameData.Resources.Image;

using ResourceExtraction.Extractors;
using ResourceExtraction.Imaging;

using System.IO;
using System.Linq;
using System.Text;

using Xunit;

/// <summary>
/// Covers the creature-bitmap pipeline reverse-engineered from PrepareCreatureBitmap (ovr167 0x5cb2d)
/// and ApplyColorSetToBitmap (seg013 0x1657c):
///   BNAMES.DAT  ─ u16 count; u16 offset[count]; u16 byteCount; data[]
///                 entry @ offset = strz name; sbyte numbers[3]; sbyte colorSet
///   bitmap      ─ &lt;name&gt;&lt;number&gt;.BMX, recolored set-wide by CS&lt;colorSet&gt;.DAT (pixel = lut[pixel])
/// The colorSet is consumed into the opaque variant key (&lt;stem&gt;_CS&lt;n&gt;) and never stored in GameData.
/// </summary>
public class CreatureBitmapTests {
    static CreatureBitmapTests() {
        // BNamesExtractor/BitmapExtractor open BinaryReader with Encoding.GetEncoding(437) (DOS CP437);
        // on non-Windows .NET the codepage requires the CodePages provider.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    // ───────────────── variant-key parsing ─────────────────

    [Theory]
    [InlineData("MOR1_CS2", "MOR1", 2)]
    [InlineData("GNT3_CS4", "GNT3", 4)]
    [InlineData("WYV1_CS0", "WYV1", 0)]
    [InlineData("GOR1", "GOR1", -1)]      // base creature, no recolor
    [InlineData("AIR2", "AIR2", -1)]
    public void ParseVariantKey_SplitsStemAndColorSet(string key, string stem, int cs) {
        (string baseStem, int colorSet) = CreatureColorSet.ParseVariantKey(key);
        Assert.Equal(stem, baseStem);
        Assert.Equal(cs, colorSet);
    }

    // ───────────────── set-wide LUT application ─────────────────

    [Fact]
    public void Apply_RemapsEveryPixelOfEverySubImage() {
        var lut = new byte[256];
        for (int i = 0; i < 256; i++) {
            lut[i] = (byte)i;       // identity baseline
        }
        lut[0xA0] = 0xD7;           // a CS-style substitution block
        lut[0xC8] = 0xB2;

        var set = new ImageSet("X");
        set.Images.Add(new BmImage("X#0") { BitMapData = [0x00, 0xA0, 0xC8, 0x05] });
        set.Images.Add(new BmImage("X#1") { BitMapData = [0xA0, 0xA0] });

        CreatureColorSet.Apply(set, lut);

        Assert.Equal([0x00, 0xD7, 0xB2, 0x05], set.Images[0].BitMapData);
        Assert.Equal([0xD7, 0xD7], set.Images[1].BitMapData);   // second sub-image recolored too
    }

    [Fact]
    public void ReadLut_ReadsExactly256Bytes() {
        var raw = new byte[300];
        for (int i = 0; i < 256; i++) {
            raw[i] = (byte)(255 - i);
        }
        byte[] lut = CreatureColorSet.ReadLut(new MemoryStream(raw));
        Assert.Equal(256, lut.Length);
        Assert.Equal(255, lut[0]);
        Assert.Equal(0, lut[255]);
    }

    // ───────────────── BNAMES extraction (synthetic) ─────────────────

    /// <summary>Builds a BNAMES.DAT blob: header + offset table + packed 8-byte entries
    /// (strz name padded to 4 + numbers[3] + colorSet).</summary>
    private static byte[] BuildBNames((string name, sbyte[] numbers, sbyte colorSet)[] entries) {
        var data = new MemoryStream();
        var offsets = new ushort[entries.Length];
        for (int i = 0; i < entries.Length; i++) {
            offsets[i] = (ushort)data.Position;
            byte[] name = System.Text.Encoding.ASCII.GetBytes(entries[i].name);
            data.Write(name, 0, name.Length);
            for (int p = name.Length; p < 4; p++) {
                data.WriteByte(0);                   // pad name field to 4 bytes
            }
            foreach (sbyte n in entries[i].numbers) {
                data.WriteByte((byte)n);
            }
            data.WriteByte((byte)entries[i].colorSet);
        }
        byte[] blob = data.ToArray();

        var output = new MemoryStream();
        var writer = new BinaryWriter(output);
        writer.Write((ushort)entries.Length);
        foreach (ushort o in offsets) {
            writer.Write(o);
        }
        writer.Write((ushort)blob.Length);
        writer.Write(blob);
        return output.ToArray();
    }

    [Fact]
    public void Extract_BakesColorSetIntoKeys_AndSkipsUnusedAndAliases() {
        byte[] blob = BuildBNames([
            ("",    [0, 0, 0], -1),        // unused slot (empty name) -> skipped
            ("gor", [1, 2, 3], -1),        // base creature -> no _CS suffix
            ("gnt", [1, 2, 3], 4),         // recolored -> _CS4 on every bitmap
            ("mor", [1, 2, -1], 2),        // negative number is an alias -> skipped
        ]);

        CreatureBitmaps result = new BNamesExtractor().Extract("BNAMES.DAT", new MemoryStream(blob));

        Assert.Equal(3, result.Creatures.Count);   // empty slot dropped

        CreatureSprite gor = result.Creatures.Single(c => c.Name == "gor");
        Assert.Equal(1, gor.CreatureId);
        Assert.Equal(["GOR1", "GOR2", "GOR3"], gor.SpriteKeys);

        CreatureSprite gnt = result.Creatures.Single(c => c.Name == "gnt");
        Assert.Equal(["GNT1_CS4", "GNT2_CS4", "GNT3_CS4"], gnt.SpriteKeys);

        CreatureSprite mor = result.Creatures.Single(c => c.Name == "mor");
        Assert.Equal(["MOR1_CS2", "MOR2_CS2"], mor.SpriteKeys);   // negative third number skipped
    }

    // ───────────────── real shipped data (skips when absent) ─────────────────

    private static string? FindOriginalGameDir() {
        string? dir = System.AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            if (File.Exists(Path.Combine(dir, "OriginalGame", "BNAMES.DAT"))) {
                return Path.Combine(dir, "OriginalGame");
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    [Fact]
    public void Extract_RealBNames_HasExpectedCreaturesAndVariants() {
        string? gameDir = FindOriginalGameDir();
        if (gameDir == null) {
            return; // shipped data absent (CI) — synthetic tests cover the logic
        }

        using FileStream stream = File.OpenRead(Path.Combine(gameDir, "BNAMES.DAT"));
        CreatureBitmaps bitmaps = new BNamesExtractor().Extract("BNAMES.DAT", stream);

        Assert.Equal(43, bitmaps.Creatures.Count);

        // base Moredhel (slot 18 is the cs2 variant; slot ... ) and a few known recolors
        Assert.Equal(["GOR1", "GOR2", "GOR3"], bitmaps.Creatures.Single(c => c.CreatureId == 15).SpriteKeys);
        Assert.Equal(["MOR1_CS2", "MOR2_CS2", "MOR3_CS2"], bitmaps.Creatures.Single(c => c.CreatureId == 18).SpriteKeys);
        Assert.Equal(["GNT1_CS4", "GNT2_CS4", "GNT3_CS4"], bitmaps.Creatures.Single(c => c.CreatureId == 31).SpriteKeys);

        // every constructed base BMX exists in the archive
        foreach (CreatureSprite c in bitmaps.Creatures) {
            foreach (string key in c.SpriteKeys) {
                (string stem, _) = CreatureColorSet.ParseVariantKey(key);
                Assert.True(File.Exists(Path.Combine(gameDir, stem + ".BMX")), $"missing {stem}.BMX for {key}");
            }
        }

        // exactly 28 distinct recolored variant assets
        int recolored = bitmaps.Creatures.SelectMany(c => c.SpriteKeys).Where(k => k.Contains("_CS")).Distinct().Count();
        Assert.Equal(28, recolored);
    }

    [Fact]
    public void Apply_RealMorBitmapWithCs2_ChangesPixels() {
        string? gameDir = FindOriginalGameDir();
        if (gameDir == null) {
            return;
        }

        using FileStream bmx = File.OpenRead(Path.Combine(gameDir, "MOR1.BMX"));
        ImageSet set = new BitmapExtractor().Extract("MOR1.BMX", bmx);
        byte[] before = (byte[])set.Images[0].BitMapData!.Clone();

        using FileStream cs = File.OpenRead(Path.Combine(gameDir, "CS2.DAT"));
        byte[] lut = CreatureColorSet.ReadLut(cs);
        CreatureColorSet.Apply(set, lut);

        byte[] after = set.Images[0].BitMapData!;
        Assert.NotEqual(before, after);                                    // recolor actually changed pixels
        for (int i = 0; i < after.Length; i++) {
            Assert.Equal(lut[before[i]], after[i]);                        // every pixel = lut[original]
        }
    }
}
