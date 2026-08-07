namespace BetrayalAtKrondor.Tests.Text;

using ResourceExtraction.Extractors.Exe;
using System.Collections.Generic;
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
        ExeStringTable t = null;
        foreach (ExeStringTable c in ExeStringManifest.Tables) {
            if (c.KeyPrefix == "attribute") { t = c; }
        }
        Assert.NotNull(t);
        Assert.Equal(16, t.Count);
        Assert.Equal(16, t.Names.Length);
        Assert.Equal("Health", t.Anchor);
    }

    [Fact]
    public void ConditionTableHasSixNamedEntries() {
        ExeStringTable t = null;
        foreach (ExeStringTable c in ExeStringManifest.Tables) {
            if (c.KeyPrefix == "condition") { t = c; }
        }
        Assert.NotNull(t);
        Assert.Equal(6, t.Count);
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
}
