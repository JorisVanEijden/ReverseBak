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
/// References covered here (all in <b>key mode</b> after the spell de-index shape change):
///  #8  Spell.ObjectKey → ObjectInfo item (base:objinfo:&lt;Number&gt;), null = "no object"
///  #9  SPELLDOC.SpellKey / SpellSymbolNode.SpellKey → Spell (base:spell:&lt;id&gt;)
///  #14 DEF_COMB / DEF_TRAP EncounterNumber → TRAPS encounter (.Index)</summary>
public class CatalogReferenceTests {
    /// <summary>#8 — every spell that names an object resolves to a live ObjectInfo entry via its
    /// de-indexed <c>ObjectKey</c>.</summary>
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
            if (spell.Value.TryGetProperty("ObjectKey", out JsonElement ok) && ok.ValueKind == JsonValueKind.String) {
                refs.Add(new ContentReference(spell.Value.GetProperty("Key").GetString()!, "objinfo", ok.GetString()!));
            }
            // ObjectKey null => sentinel -1 (no associated object); nothing to resolve.
        }

        AssertAllResolve(catalogs, refs);
    }

    /// <summary>#9 — every SPELLDOC entry and every spell-symbol node resolve to a live spell via their
    /// de-indexed <c>SpellKey</c>.</summary>
    [Fact]
    public void EverySpellDocAndSymbol_ReferenceAValidSpell() {
        string? gen = GeneratedCorpus.FindDir(Path.Combine("DAT", "spells.json"), "SPELLDOC.json", "SYMBOL");
        if (gen == null) {
            return;
        }

        var catalogs = new Dictionary<string, ISet<string>> {
            ["spell"] = SpellKeys(gen),
        };

        var refs = new List<ContentReference>();
        AddSpellKeyRefs(refs, Path.Combine(gen, "SPELLDOC.json"), "Spells", "spelldoc");
        foreach (string symbolPath in Directory.GetFiles(Path.Combine(gen, "SYMBOL"), "SYMBOL*.json")) {
            AddSpellKeyRefs(refs, symbolPath, "Nodes", Path.GetFileNameWithoutExtension(symbolPath).ToLowerInvariant());
        }

        AssertAllResolve(catalogs, refs);
    }

    /// <summary>#9 (affinity tables) — SPELLWEA (weaknesses) and SPELLRES (resistances) are parallel
    /// positional tables. Both self-declare 64 records while the real spell count is 45. RESOLVED (IDA,
    /// docs/work-todo.md): the spell keyspace is <b>45</b>; the 64 is over-allocation and slots 45..63
    /// are never read at runtime (dead authoring data — <c>Cast_Spell</c> indexes the 45-record
    /// spell-data array with the same spellNumber, so spellNumber is always 0..44). Records 0..44 are
    /// the real per-spell affinity data; only their alignment is worth enforcing here — the two tables
    /// must stay structurally aligned (same 64-count, same spell-number set).</summary>
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

    /// <summary>Creature-graph closure — every MONSTxx stats file's <c>CreatureKey</c> resolves to a
    /// live creature. The MONST file number IS the mnames creature number (inventory caveat 1), so
    /// MonsterStats, EnemySlot, and SpellAffinity all key to the one <c>base:mnames</c> catalog.</summary>
    [Fact]
    public void EveryMonsterStats_ReferencesAValidCreature() {
        string? gen = GeneratedCorpus.FindDir(Path.Combine("DAT", "mnames.json"), "DAT");
        if (gen == null) {
            return;
        }

        var mnamesKeys = new HashSet<string>();
        using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(gen, "DAT", "mnames.json")))) {
            foreach (JsonElement c in doc.RootElement.GetProperty("Creatures").EnumerateArray()) {
                mnamesKeys.Add(c.GetProperty("Key").GetString()!);
            }
        }
        var catalogs = new Dictionary<string, ISet<string>> { ["mnames"] = mnamesKeys };

        var refs = new List<ContentReference>();
        foreach (string monstPath in Directory.GetFiles(Path.Combine(gen, "DAT"), "MONST*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(monstPath));
            refs.Add(new ContentReference($"base:monst:{Path.GetFileNameWithoutExtension(monstPath)}",
                "mnames", doc.RootElement.GetProperty("CreatureKey").GetString()!));
        }

        AssertAllResolve(catalogs, refs);
        Assert.True(refs.Count > 0, "Found no MONSTxx stats files.");
    }

    /// <summary>#10 — every spell-affinity creature-type (SPELLWEA/SPELLRES) resolves to a live
    /// creature via its de-indexed <c>CreatureKey</c>. The creature-type index is the mnames creature
    /// number (resolved: the three creature-numbering schemes are one — inventory caveat 1).</summary>
    [Fact]
    public void EveryAffinityCreatureType_ReferencesAValidCreature() {
        string? gen = GeneratedCorpus.FindDir(Path.Combine("DAT", "mnames.json"), "SPELLWEA.json", "SPELLRES.json");
        if (gen == null) {
            return;
        }

        var mnamesKeys = new HashSet<string>();
        using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(gen, "DAT", "mnames.json")))) {
            foreach (JsonElement c in doc.RootElement.GetProperty("Creatures").EnumerateArray()) {
                mnamesKeys.Add(c.GetProperty("Key").GetString()!);
            }
        }
        var catalogs = new Dictionary<string, ISet<string>> { ["mnames"] = mnamesKeys };

        var refs = new List<ContentReference>();
        foreach (string table in new[] { "SPELLWEA.json", "SPELLRES.json" }) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(gen, table)));
            string from = Path.GetFileNameWithoutExtension(table).ToLowerInvariant();
            foreach (JsonElement spell in doc.RootElement.GetProperty("Spells").EnumerateArray()) {
                int n = spell.GetProperty("SpellNumber").GetInt32();
                foreach (JsonElement ck in spell.GetProperty("CreatureKeys").EnumerateArray()) {
                    refs.Add(new ContentReference($"base:{from}:{n}", "mnames", ck.GetString()!));
                }
            }
        }

        AssertAllResolve(catalogs, refs);
        Assert.True(refs.Count > 0, "Found no affinity creature-type references.");
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

    // objinfo catalog: canonical base:objinfo:<Number> keys (matches ObjectInfoContentSource; the
    // objinfo entry's stable identity is its Number, so keys are Number-derived).
    private static HashSet<string> ObjectInfoKeys(string gen) {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(gen, "ObjectInfo", "objinfo.json")));
        var keys = new HashSet<string>();
        foreach (JsonElement o in doc.RootElement.EnumerateArray()) {
            keys.Add(ContentKey.ForBase("objinfo", o.GetProperty("Number").GetInt32()));
        }
        return keys;
    }

    // spell catalog: the emitted Spell.Key values (base:spell:<id>).
    private static HashSet<string> SpellKeys(string gen) {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(gen, "DAT", "spells.json")));
        var keys = new HashSet<string>();
        foreach (JsonProperty spell in doc.RootElement.GetProperty("Spells").EnumerateObject()) {
            keys.Add(spell.Value.GetProperty("Key").GetString()!);
        }
        return keys;
    }

    private static HashSet<string> TrapEncounterKeys(string gen) {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(gen, "TRAPS.json")));
        var keys = new HashSet<string>();
        foreach (JsonElement enc in doc.RootElement.GetProperty("Encounters").EnumerateArray()) {
            keys.Add(enc.GetProperty("Key").GetString()!);
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

    // Collect SpellKey references from a list of entries (SPELLDOC "Spells" or SYMBOL "Nodes").
    private static void AddSpellKeyRefs(List<ContentReference> refs, string path, string listProp, string fromPrefix) {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        int i = 0;
        foreach (JsonElement e in doc.RootElement.GetProperty(listProp).EnumerateArray()) {
            refs.Add(new ContentReference($"base:{fromPrefix}:{i++}", "spell", e.GetProperty("SpellKey").GetString()!));
        }
    }

    private static void AddDefEncounterRefs(List<ContentReference> refs, string path, string fromPrefix) {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        int idx = 0;
        foreach (JsonElement rec in doc.RootElement.GetProperty("Records").EnumerateArray()) {
            if (rec.TryGetProperty("Payload", out JsonElement payload)
                && payload.ValueKind == JsonValueKind.Object
                && payload.TryGetProperty("EncounterKey", out JsonElement enc)) {
                refs.Add(new ContentReference($"base:{fromPrefix}:{idx}", "traps", enc.GetString()!));
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
