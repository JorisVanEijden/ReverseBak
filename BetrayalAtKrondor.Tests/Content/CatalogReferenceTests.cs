namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GameData.Resources.Content;
using Xunit;

/// <summary>Integration enforcement of the global-catalog references from
/// docs/re-notes/reference-inventory.md, over the committed <c>generated/</c> corpus, via
/// <see cref="ReferenceValidator"/>. Each test runs in "index in-bounds" mode today (target keys are
/// the target list's indices/ids); after the extractors de-index each reference to a real key, the
/// same declarations enforce key resolution unchanged. Skip-if-absent (see <see cref="GeneratedCorpus"/>).
///
/// References covered here:
///  #8  Spell.ObjectId → ObjectInfo item (.Number), sentinel -1 = "no object"
///  #9  SPELLDOC.SpellNumber / SPELLWEA.SpellNumber → Spell (.Id)
///  #14 DEF_COMB / DEF_TRAP EncounterNumber → TRAPS encounter (.Index)</summary>
public class CatalogReferenceTests {
    /// <summary>#8 — every spell that names an object points at a live ObjectInfo entry.</summary>
    [Fact]
    public void EverySpellObjectId_ReferencesAValidObjectInfoEntry() {
        string? gen = GeneratedCorpus.FindDir(Path.Combine("DAT", "spells.json"),
                                              Path.Combine("ObjectInfo", "objinfo.json"));
        if (gen == null) {
            return;
        }

        var catalogs = new Dictionary<string, ISet<string>> {
            ["objinfo"] = ObjectInfoKeys(gen),
        };

        var refs = new List<ContentReference>();
        using JsonDocument spells = JsonDocument.Parse(File.ReadAllText(Path.Combine(gen, "DAT", "spells.json")));
        foreach (JsonProperty spell in spells.RootElement.GetProperty("Spells").EnumerateObject()) {
            int objectId = spell.Value.GetProperty("ObjectId").GetInt32();
            if (objectId < 0) {
                continue; // sentinel: -1 = spell has no associated object.
            }
            refs.Add(new ContentReference($"base:spell:{spell.Name}", "objinfo", objectId.ToString()));
        }

        AssertAllResolve(catalogs, refs);
    }

    /// <summary>#9 — every SPELLDOC entry points at a live spell. SPELLDOC is the human-readable spell
    /// description table; it is 1:1 with the player-castable spell catalog (both 45), so this is the
    /// clean reference gate.</summary>
    [Fact]
    public void EverySpellDoc_ReferencesAValidSpell() {
        string? gen = GeneratedCorpus.FindDir(Path.Combine("DAT", "spells.json"), "SPELLDOC.json");
        if (gen == null) {
            return;
        }

        var catalogs = new Dictionary<string, ISet<string>> {
            ["spell"] = SpellKeys(gen),
        };

        var refs = new List<ContentReference>();
        AddSpellNumberRefs(refs, Path.Combine(gen, "SPELLDOC.json"), "spelldoc");

        AssertAllResolve(catalogs, refs);
    }

    /// <summary>#9 (affinity tables) — SPELLWEA (weaknesses) and SPELLRES (resistances) are parallel
    /// positional tables keyed by spell number. NOTE (documented in docs/work-todo.md): both declare a
    /// 64-slot spell keyspace, wider than the 45 player-castable spells in spells.dat — the tail
    /// (45..63) carries real creature-affinity data (e.g. SPELLRES #58 spans all 48 creature types).
    /// So they do NOT resolve into the 45-spell catalog; the enforceable invariant is that the two
    /// tables stay structurally aligned (same count, same spell-number set). Keying spells must adopt
    /// the 64-slot keyspace, not the 45 from spells.dat.</summary>
    [Fact]
    public void SpellAffinityTables_ShareTheSameSpellKeyspace() {
        string? gen = GeneratedCorpus.FindDir("SPELLWEA.json", "SPELLRES.json");
        if (gen == null) {
            return;
        }

        ISet<string> weaKeys = SpellNumberKeys(Path.Combine(gen, "SPELLWEA.json"));
        ISet<string> resKeys = SpellNumberKeys(Path.Combine(gen, "SPELLRES.json"));

        // Cross-validate each table's keyspace against the other via ReferenceValidator: every SPELLWEA
        // row must have a matching SPELLRES row and vice versa. Divergence (a re-extraction that drops
        // or misaligns rows in one table) breaks the gate.
        var catalogs = new Dictionary<string, ISet<string>> { ["affinity"] = resKeys };
        var refs = weaKeys.Select(k => new ContentReference($"base:spellwea:{k}", "affinity", k)).ToList();
        AssertAllResolve(catalogs, refs);

        catalogs = new Dictionary<string, ISet<string>> { ["affinity"] = weaKeys };
        refs = resKeys.Select(k => new ContentReference($"base:spellres:{k}", "affinity", k)).ToList();
        AssertAllResolve(catalogs, refs);
    }

    /// <summary>#14 — every DEF_COMB / DEF_TRAP record's EncounterNumber points at a live TRAPS encounter.</summary>
    [Fact]
    public void EveryDefEncounterNumber_ReferencesAValidTrapsEncounter() {
        string? gen = GeneratedCorpus.FindDir("TRAPS.json",
                                              Path.Combine("DEF", "DEF_COMB.json"),
                                              Path.Combine("DEF", "DEF_TRAP.json"));
        if (gen == null) {
            return;
        }

        var catalogs = new Dictionary<string, ISet<string>> {
            ["traps"] = TrapEncounterKeys(gen),
        };

        var refs = new List<ContentReference>();
        AddDefEncounterRefs(refs, Path.Combine(gen, "DEF", "DEF_COMB.json"), "def_comb");
        AddDefEncounterRefs(refs, Path.Combine(gen, "DEF", "DEF_TRAP.json"), "def_trap");

        AssertAllResolve(catalogs, refs);
    }

    private static HashSet<string> ObjectInfoKeys(string gen) {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(gen, "ObjectInfo", "objinfo.json")));
        var keys = new HashSet<string>();
        foreach (JsonElement o in doc.RootElement.EnumerateArray()) {
            keys.Add(o.GetProperty("Number").GetInt32().ToString());
        }
        return keys;
    }

    private static HashSet<string> SpellKeys(string gen) {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(gen, "DAT", "spells.json")));
        var keys = new HashSet<string>();
        foreach (JsonProperty spell in doc.RootElement.GetProperty("Spells").EnumerateObject()) {
            keys.Add(spell.Name); // key = spell id ("0".."44")
        }
        return keys;
    }

    private static HashSet<string> TrapEncounterKeys(string gen) {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(gen, "TRAPS.json")));
        var keys = new HashSet<string>();
        foreach (JsonElement enc in doc.RootElement.GetProperty("Encounters").EnumerateArray()) {
            keys.Add(enc.GetProperty("Index").GetInt32().ToString());
        }
        return keys;
    }

    private static HashSet<string> SpellNumberKeys(string path) {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        var keys = new HashSet<string>();
        foreach (JsonElement e in doc.RootElement.GetProperty("Spells").EnumerateArray()) {
            keys.Add(e.GetProperty("SpellNumber").GetInt32().ToString());
        }
        return keys;
    }

    private static void AddSpellNumberRefs(List<ContentReference> refs, string path, string fromPrefix) {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonElement e in doc.RootElement.GetProperty("Spells").EnumerateArray()) {
            int n = e.GetProperty("SpellNumber").GetInt32();
            refs.Add(new ContentReference($"base:{fromPrefix}:{n}", "spell", n.ToString()));
        }
    }

    private static void AddDefEncounterRefs(List<ContentReference> refs, string path, string fromPrefix) {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        int idx = 0;
        foreach (JsonElement rec in doc.RootElement.GetProperty("Records").EnumerateArray()) {
            if (rec.TryGetProperty("Payload", out JsonElement payload)
                && payload.ValueKind == JsonValueKind.Object
                && payload.TryGetProperty("EncounterNumber", out JsonElement enc)) {
                refs.Add(new ContentReference($"base:{fromPrefix}:{idx}", "traps", enc.GetInt32().ToString()));
            }
            idx++;
        }
    }

    private static void AssertAllResolve(Dictionary<string, ISet<string>> catalogs, List<ContentReference> refs) {
        IReadOnlyList<BrokenReference> broken = ReferenceValidator.Validate(catalogs, refs);
        Assert.True(broken.Count == 0,
            $"{broken.Count} references do not resolve. First few: " +
            string.Join("; ", broken.Take(5).Select(b => $"{b.FromKey} → {b.TargetCatalog}:{b.TargetKey}")));
    }
}
