namespace BetrayalAtKrondor.Tests.Text;

using ResourceExtraction.Extractors.Exe;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

public class ExeStringManifestTests {
    [Fact]
    public void DeclaresTheTwoDisplayTables() {
        Assert.Equal(2, ExeStringManifest.Tables.Count);
    }

    // 16, not 17: ActorAttribute's extra HealthStaminaCombo is a derived pseudo-attribute
    // the original never displays and never named.
    [Fact]
    public void AttributeTableHasSixteenNamedEntries() {
        ExeStringTable? t = null;
        foreach (ExeStringTable c in ExeStringManifest.Tables) {
            if (c.KeyPrefix == "attribute") { t = c; }
        }
        Assert.NotNull(t);
        Assert.Equal(16, t!.Count);
        Assert.Equal(16, t.Names.Length);
        Assert.Equal("Health", t.Anchor);
    }

    [Fact]
    public void ConditionTableHasSixNamedEntries() {
        ExeStringTable? t = null;
        foreach (ExeStringTable c in ExeStringManifest.Tables) {
            if (c.KeyPrefix == "condition") { t = c; }
        }
        Assert.NotNull(t);
        Assert.Equal(6, t!.Count);
        Assert.Equal(6, t.Names.Length);
    }

    // Every declared key must be unique — a duplicate would silently drop an entry.
    [Fact]
    public void EveryDeclaredKeyIsUnique() {
        var seen = new HashSet<string>();
        foreach (ExeStringTable t in ExeStringManifest.Tables) {
            foreach (string n in t.Names) {
                Assert.True(seen.Add($"base:uistring:{t.KeyPrefix}.{n}"), $"duplicate {t.KeyPrefix}.{n}");
            }
        }
        foreach (ExeStringSingle s in ExeStringManifest.Singles) {
            Assert.True(seen.Add(s.Key), $"duplicate {s.Key}");
        }
    }

    [Fact]
    public void EveryKeyUsesTheDeclaredPrefixAndSnakeCase() {
        foreach (ExeStringSingle s in ExeStringManifest.Singles) {
            Assert.StartsWith("base:uistring:", s.Key);
            Assert.DoesNotContain(" ", s.Key);
            Assert.Equal(s.Key.ToLowerInvariant(), s.Key);
        }
    }

    // Anchor and Count are derived from the entries, not declared beside them, so the classic
    // "edited the names, forgot the count" drift has nowhere to live.
    [Fact]
    public void TableAnchorAndCountAreDerivedFromTheEntries() {
        foreach (ExeStringTable t in ExeStringManifest.Tables) {
            Assert.Equal(t.Entries.Length, t.Count);
            Assert.Equal(t.Entries[0].Text, t.Anchor);
            Assert.Equal(t.Entries.Length, t.Names.Length);
            foreach (ExeStringTableEntry e in t.Entries) {
                Assert.False(string.IsNullOrEmpty(e.Name));
                Assert.False(string.IsNullOrEmpty(e.Text));
            }
        }
    }

    // A NUL-padded fixed-stride table, as the executable stores them.
    private static byte[] TableImage(IEnumerable<string> entries, int stride) {
        var bytes = new List<byte>();
        foreach (string e in entries) {
            byte[] raw = Encoding.ASCII.GetBytes(e);
            bytes.AddRange(raw);
            for (int i = raw.Length; i < stride; i++) {
                bytes.Add(0);
            }
        }
        return bytes.ToArray();
    }

    // Spec §6: a table entry whose contents do not match its declaration must throw, naming the
    // table, the index, the expected text and the actual one. Only entry 0 is anchored; every
    // later slot is reached by arithmetic, so without this check a wrong stride or a different
    // build fills the catalog with plausible-looking garbage and nothing downstream can tell.
    [Fact]
    public void ExtractThrowsWhenATableEntryDoesNotMatchItsDeclaration() {
        // The condition table is the first one Extract walks, so a mismatch here fires before any
        // singleton is looked for — the image needs nothing else in it.
        byte[] exe = TableImage(
            new[] { "Plagued", "Poisoned", "Sozzled", "Healing", "Starving", "Near-death" }, 23);

        InvalidDataException ex = Assert.Throws<InvalidDataException>(
            () => ExeStringManifest.Extract(exe));

        Assert.Contains("condition", ex.Message);   // the table
        Assert.Contains("index 2", ex.Message);     // the index
        Assert.Contains("drunk", ex.Message);       // the declaration's key
        Assert.Contains("Drunk", ex.Message);       // the expected text
        Assert.Contains("Sozzled", ex.Message);     // the actual text
    }

    // The happy path of the same check: a table whose contents match is read through unchanged.
    [Fact]
    public void ExtractAcceptsATableThatMatchesItsDeclaration() {
        byte[] exe = TableImage(
            new[] { "Plagued", "Poisoned", "Drunk", "Healing", "Starving", "Near-death" }, 23);

        // The attribute table and every singleton are still absent, so extraction fails — but on
        // the NEXT declaration, proving the condition table itself passed.
        InvalidDataException ex = Assert.Throws<InvalidDataException>(
            () => ExeStringManifest.Extract(exe));
        Assert.DoesNotContain("condition", ex.Message);
    }
}
