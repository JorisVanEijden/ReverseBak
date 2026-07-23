namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using GameData.Resources.Content;
using Xunit;

public class ReferenceValidatorTests {
    private static Dictionary<string, ISet<string>> Catalogs(params (string name, string[] keys)[] cats) {
        var d = new Dictionary<string, ISet<string>>();
        foreach (var (name, keys) in cats) d[name] = new HashSet<string>(keys);
        return d;
    }

    [Fact] public void AllReferencesResolve_NoBroken() {
        var cats = Catalogs(("tbl:z01", new[] { "base:tbl:z01:house", "base:tbl:z01:tree" }));
        var refs = new[] {
            new ContentReference("base:wld:01:10:12:a", "tbl:z01", "base:tbl:z01:house"),
            new ContentReference("base:wld:01:10:12:b", "tbl:z01", "base:tbl:z01:tree"),
        };
        Assert.Empty(ReferenceValidator.Validate(cats, refs));
    }

    [Fact] public void DanglingTargetKey_IsReported() {
        var cats = Catalogs(("tbl:z01", new[] { "base:tbl:z01:house" }));
        var refs = new[] { new ContentReference("wld:x", "tbl:z01", "base:tbl:z01:ghost") };
        BrokenReference b = Assert.Single(ReferenceValidator.Validate(cats, refs));
        Assert.Equal("wld:x", b.FromKey);
        Assert.Equal("tbl:z01", b.TargetCatalog);
        Assert.Equal("base:tbl:z01:ghost", b.TargetKey);
    }

    [Fact] public void UnknownTargetCatalog_IsReported() {
        var cats = Catalogs(("tbl:z01", new[] { "base:tbl:z01:house" }));
        var refs = new[] { new ContentReference("wld:x", "tbl:z99", "base:tbl:z99:house") };
        Assert.Single(ReferenceValidator.Validate(cats, refs));
    }

    [Fact] public void ModAddedTargetKey_Resolves() {
        // A mod added `mymod:magictree` to the zone's TBL catalog; a reference to it resolves.
        var cats = Catalogs(("tbl:z01", new[] { "base:tbl:z01:house", "mymod:magictree" }));
        var refs = new[] { new ContentReference("mymod:wld:1", "tbl:z01", "mymod:magictree") };
        Assert.Empty(ReferenceValidator.Validate(cats, refs));
    }

    [Fact] public void EmptyReferences_NoBroken() =>
        Assert.Empty(ReferenceValidator.Validate(Catalogs(), new ContentReference[0]));
}
