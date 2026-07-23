namespace GameData.Resources.Content;

using System.Collections.Generic;

/// <summary>The "don't miss anything" linchpin: given the content graph as catalogs (name → the set
/// of keys present, base ∪ mods) and the declared references (edges), reports every reference whose
/// target key does not resolve — a dangling reference, or one that was never de-indexed. Run over
/// the whole corpus, this converts "did I miss a reference?" from judgment into a total check.
/// Pure: no I/O, no Unity, no logger. See the design spec
/// docs/superpowers/specs/2026-07-23-keyed-content-graph-and-reference-validator.md.</summary>
public static class ReferenceValidator {
    public static IReadOnlyList<BrokenReference> Validate(
        IReadOnlyDictionary<string, ISet<string>> catalogs,
        IEnumerable<ContentReference> references) {
        var broken = new List<BrokenReference>();
        foreach (ContentReference r in references) {
            bool resolves = catalogs.TryGetValue(r.TargetCatalog, out ISet<string>? keys)
                            && keys.Contains(r.TargetKey);
            if (!resolves) {
                broken.Add(new BrokenReference(r.FromKey, r.TargetCatalog, r.TargetKey));
            }
        }
        return broken;
    }
}
