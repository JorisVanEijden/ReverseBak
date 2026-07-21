namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using GameData.Resources.Content;
using Xunit;

public class ContentRegistryTests {
    // Minimal in-test source. Positional record auto-implements IContentSource<T>.
    private sealed record Src<T>(string SourceName, IReadOnlyList<ContentEntry<T>> Entries) : IContentSource<T>;

    private static Src<string> Source(string name, params (string key, string val)[] entries) {
        var list = new List<ContentEntry<string>>();
        foreach (var (key, val) in entries) list.Add(new ContentEntry<string>(key, val));
        return new Src<string>(name, list);
    }

    [Fact] public void Merge_AddsDisjointKeysFromAllSources() {
        MergedCatalog<string> c = ContentRegistry.Merge(new List<IContentSource<string>> {
            Source("base", ("base:objinfo:1", "sword")),
            Source("mod", ("mod:antidote", "antidote")),
        });
        Assert.Equal(2, c.Count);
        Assert.True(c.TryGet("base:objinfo:1", out string s)); Assert.Equal("sword", s);
        Assert.True(c.TryGet("mod:antidote", out string a)); Assert.Equal("antidote", a);
        Assert.Empty(c.Overrides);
    }

    [Fact] public void Merge_LaterSourceWinsOnSharedKey_AndRecordsOverride() {
        MergedCatalog<string> c = ContentRegistry.Merge(new List<IContentSource<string>> {
            Source("base", ("base:objinfo:1", "sword")),
            Source("mod", ("base:objinfo:1", "excalibur")),
        });
        Assert.Equal("excalibur", c.Entries["base:objinfo:1"]);
        Assert.Equal("mod", c.Provenance["base:objinfo:1"]);
        KeyOverride ov = Assert.Single(c.Overrides);
        Assert.Equal(new KeyOverride("base:objinfo:1", "base", "mod"), ov);
    }

    [Fact] public void Merge_IsSourceOrderDependent() {
        var a = Source("A", ("k", "a"));
        var b = Source("B", ("k", "b"));
        Assert.Equal("b", ContentRegistry.Merge(new List<IContentSource<string>> { a, b }).Entries["k"]);
        Assert.Equal("a", ContentRegistry.Merge(new List<IContentSource<string>> { b, a }).Entries["k"]);
    }

    [Fact] public void Merge_EmptySources_YieldEmptyCatalog() {
        MergedCatalog<string> c = ContentRegistry.Merge(new List<IContentSource<string>>());
        Assert.Equal(0, c.Count);
        Assert.Empty(c.Overrides);
    }
}
