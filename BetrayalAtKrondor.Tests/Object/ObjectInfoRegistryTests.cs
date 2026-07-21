namespace BetrayalAtKrondor.Tests.Object;

using System.Collections.Generic;
using GameData.Resources.Content;
using GameData.Resources.Object;
using Xunit;

public class ObjectInfoRegistryTests {
    private static ObjectInfoSet Set(params (int number, string name)[] items) {
        var list = new List<ObjectInfo>();
        foreach (var (n, name) in items) list.Add(new ObjectInfo("OBJINFO.DAT") { Number = n, Name = name });
        return new ObjectInfoSet("OBJINFO.DAT", list);
    }

    [Fact] public void Source_YieldsCanonicalBaseKeyPerItem() {
        var src = new ObjectInfoContentSource(Set((0, "knife"), (17, "sword")));
        Assert.Equal("base:objinfo", src.SourceName);
        var keys = new List<string>();
        foreach (ContentEntry<ObjectInfo> e in src.Entries) keys.Add(e.Key);
        Assert.Contains("base:objinfo:0", keys);
        Assert.Contains("base:objinfo:17", keys);
    }

    // Spec §7 no-regression invariant: with only the base source, the merged catalog's GetById
    // is behaviorally identical to the original ObjectInfoSet.
    [Fact] public void BaseOnlyMerge_GetById_MatchesOriginalSet() {
        ObjectInfoSet set = Set((0, "knife"), (17, "sword"), (42, "shield"));
        MergedCatalog<ObjectInfo> merged = ContentRegistry.Merge(
            new List<IContentSource<ObjectInfo>> { new ObjectInfoContentSource(set) });
        var catalog = new ObjectInfoCatalog(merged);
        foreach (ObjectInfo item in set.Items)
            Assert.Same(set.GetById(item.Number), catalog.GetById(item.Number));
        Assert.Null(catalog.GetById(999)); // absent id
    }

    [Fact] public void ModPartial_AddsNewItem_OriginalsIntact() {
        ObjectInfoSet set = Set((0, "knife"));
        var modItem = new ObjectInfo("mod") { Number = 200, Name = "antidote" };
        var mod = new ListContentSource<ObjectInfo>("testmod",
            new[] { new ContentEntry<ObjectInfo>("testmod:antidote", modItem) });
        MergedCatalog<ObjectInfo> merged = ContentRegistry.Merge(
            new List<IContentSource<ObjectInfo>> { new ObjectInfoContentSource(set), mod });
        Assert.Same(modItem, merged.Entries["testmod:antidote"]);
        // Original still addressable by its numeric id; mod item is not (it has no base slot).
        var catalog = new ObjectInfoCatalog(merged);
        Assert.Same(set.GetById(0), catalog.GetById(0));
        Assert.Null(catalog.GetById(200));
    }
}
