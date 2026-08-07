namespace BetrayalAtKrondor.Tests.Text;

using GameData.Resources.Text;
using Xunit;

public class UiStringsTests {
    [Fact]
    public void ParsesAFlatKeyValueDocument() {
        UiStringCatalog c = UiStringCatalog.FromJson("{\"a.b\":\"Hello\"}");
        Assert.Equal("Hello", c.Get("a.b"));
    }

    // A missing key yields empty, never the key itself: a key leaking onto the screen is
    // the exact failure mode this whole feature exists to remove.
    [Fact]
    public void MissingKeyYieldsEmpty() =>
        Assert.Equal("", UiStringCatalog.FromJson("{}").Get("nope"));

    // Later source wins — the same rule ContentRegistry.Merge uses, so a translation mod
    // can replace some entries without restating the whole catalog.
    [Fact]
    public void MergeLetsTheOverrideWinPerEntry() {
        UiStringCatalog base_ = UiStringCatalog.FromJson("{\"a\":\"1\",\"b\":\"2\"}");
        UiStringCatalog merged = base_.Merge(UiStringCatalog.FromJson("{\"b\":\"two\"}"));
        Assert.Equal("1", merged.Get("a"));
        Assert.Equal("two", merged.Get("b"));
    }

    [Fact]
    public void AmbientCatalogIsReplaceable() {
        UiStringCatalog previous = UiStrings.Catalog;
        try {
            UiStrings.Catalog = UiStringCatalog.FromJson("{\"k\":\"v\"}");
            Assert.Equal("v", UiStrings.Get("k"));
        } finally {
            UiStrings.Catalog = previous;
        }
    }

    // Verify the embedded resource is actually present in the assembly.
    [Fact]
    public void EmbeddedResourceIsEmbedded() {
        var asm = typeof(UiStringCatalog).Assembly;
        var names = asm.GetManifestResourceNames();
        Assert.Contains(names, name => name.EndsWith(UiStringCatalog.ResourceId, StringComparison.Ordinal));
    }

    // Verify that accessing the embedded catalog does not throw and returns an empty
    // catalog when the placeholder {} is in place.
    [Fact]
    public void EmbeddedReturnsEmptyCatalogForPlaceholder() {
        UiStringCatalog embedded = UiStringCatalog.Embedded;
        Assert.NotNull(embedded);
        Assert.Empty(embedded.Entries);
    }
}
