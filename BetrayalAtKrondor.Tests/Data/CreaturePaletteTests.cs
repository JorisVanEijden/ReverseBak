namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Palette;

using ResourceExtraction.Extractors;
using ResourceExtraction.Imaging;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Xunit;

/// <summary>Guards the hardcoded <see cref="CreaturePalette"/>: proves the normal-surface light zones
/// (Z01-Z05, Z07) are byte-identical, recomputes the creature-used palette + magenta fill from the shipped
/// data, and asserts the committed array still matches (so it can't silently drift).</summary>
public class CreaturePaletteTests {
    static CreaturePaletteTests() { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); }

    private static string? Dir() {
        string? d = System.AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(d)) {
            if (File.Exists(Path.Combine(d, "OriginalGame", "BNAMES.DAT"))) return Path.Combine(d, "OriginalGame");
            d = Path.GetDirectoryName(d);
        }
        return null;
    }

    [Fact]
    public void HardcodedPalette_MatchesShippedLightZones() {
        string? g = Dir();
        if (g == null) return;

        var px = new PaletteExtractor();
        Color[] Pal(string n) { using var s = File.OpenRead(Path.Combine(g, n)); return px.Extract(n, s).Colors; }

        // certainty: the normal-surface light zones are byte-identical
        string[] light = ["Z01", "Z02", "Z03", "Z04", "Z05", "Z07"];
        Color[][] lp = light.Select(z => Pal(z + ".PAL")).ToArray();
        for (int i = 0; i < 256; i++) {
            for (int z = 1; z < lp.Length; z++) {
                Assert.True(lp[0][i].Equals(lp[z][i]), $"light zones diverge at 0x{i:x2}");
            }
        }

        // recompute used set + expected palette and compare to the committed array
        var used = new bool[256];
        var bx = new BitmapExtractor();
        var lc = new Dictionary<int, byte[]>();
        byte[] Lut(int cs) {
            if (!lc.TryGetValue(cs, out byte[]? l)) { using var s = File.OpenRead(Path.Combine(g, $"CS{cs}.DAT")); l = CreatureColorSet.ReadLut(s); lc[cs] = l; }
            return l;
        }
        using (var bn = File.OpenRead(Path.Combine(g, "BNAMES.DAT"))) {
            foreach (var c in new BNamesExtractor().Extract("BNAMES.DAT", bn).Creatures) {
                foreach (var k in c.SpriteKeys) {
                    (string stem, int cs) = CreatureColorSet.ParseVariantKey(k);
                    using var s = File.OpenRead(Path.Combine(g, stem + ".BMX"));
                    var set = bx.Extract(stem + ".BMX", s);
                    if (cs >= 0) CreatureColorSet.Apply(set, Lut(cs));
                    foreach (var im in set.Images) foreach (var b in im.BitMapData!) used[b] = true;
                }
            }
        }
        var magenta = new Color(255, 0, 255);
        for (int i = 0; i < 256; i++) {
            Assert.Equal(used[i] ? lp[0][i] : magenta, CreaturePalette.Colors[i]);
        }
        Assert.Equal(256, CreaturePalette.Colors.Length);
    }

    [Fact]
    public void UnusedEntriesAreMagenta() {
        Assert.Contains(new Color(255, 0, 255), CreaturePalette.Colors);   // tripwire colour present
        Assert.Equal(256, CreaturePalette.Colors.Length);
    }
}
