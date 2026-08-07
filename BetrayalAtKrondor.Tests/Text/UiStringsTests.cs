namespace BetrayalAtKrondor.Tests.Text;

using System.IO;
using GameData.Resources.Text;
using Xunit;

public class UiStringsTests {
    [Fact]
    public void ParsesAFlatKeyValueDocument() {
        UiStringCatalog c = UiStringCatalog.FromJson("{\"a.b\":\"Hello\"}");
        Assert.Equal("Hello", c.Get("a.b"));
    }

    // FromJson is a parse function: malformed input must throw rather than be swallowed, so
    // a broken mod-supplied file can't silently ship a half-empty UI. The catch-and-fall-back
    // safety net belongs at the call site (UiStringLoader), not here.
    [Fact]
    public void FromJsonThrowsOnMalformedInput() =>
        Assert.Throws<System.Text.Json.JsonException>(() => UiStringCatalog.FromJson("{not json"));

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

    // Merge(null) is the no-mod-override case — it must behave like merging an empty
    // catalog, not throw, so callers don't need a null check before every merge.
    [Fact]
    public void MergeWithNullReturnsEquivalentCatalog() {
        UiStringCatalog base_ = UiStringCatalog.FromJson("{\"a\":\"1\",\"b\":\"2\"}");
        UiStringCatalog merged = base_.Merge(null);
        Assert.Equal("1", merged.Get("a"));
        Assert.Equal("2", merged.Get("b"));
        Assert.Equal(base_.Entries.Count, merged.Entries.Count);
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

    // A naive "Embedded returns an empty catalog for the {} placeholder" test is unfalsifiable:
    // it passes whether Embedded actually reads the manifest stream, or silently takes its
    // `name == null` fallback path, because both produce an empty catalog while the placeholder
    // is in place. Once the placeholder is replaced with ~112 real entries it would ALSO pass
    // for the wrong reason (an empty fallback catalog just looks "populated" if compared to
    // nothing). So instead: locate the manifest resource ourselves, prove the stream exists,
    // parse it independently of Embedded, and compare entry counts. If Embedded ever took the
    // fallback while a real resource existed, the counts diverge; if the resource were missing
    // entirely, the stream assertion below fails first.
    [Fact]
    public void EmbeddedReadsThroughToTheManifestResource() {
        var asm = typeof(UiStringCatalog).Assembly;
        string name = null;
        foreach (string candidate in asm.GetManifestResourceNames()) {
            if (candidate.EndsWith(UiStringCatalog.ResourceId, StringComparison.Ordinal)) {
                name = candidate;
            }
        }
        Assert.NotNull(name);

        using Stream stream = asm.GetManifestResourceStream(name);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        UiStringCatalog fromStream = UiStringCatalog.FromJson(reader.ReadToEnd());

        Assert.Equal(fromStream.Entries.Count, UiStringCatalog.Embedded.Entries.Count);
    }
}
