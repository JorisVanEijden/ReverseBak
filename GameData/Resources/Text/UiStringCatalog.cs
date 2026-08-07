namespace GameData.Resources.Text;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

/// <summary>
/// Player-visible strings lifted out of KRONDOR.EXE, keyed as
/// <c>base:uistring:&lt;group&gt;.&lt;name&gt;</c>. Shipped as an embedded resource rather than read
/// from the executable at runtime — see docs/superpowers/specs/2026-08-07-exe-ui-string-catalog-design.md.
/// </summary>
public sealed class UiStringCatalog {
    public const string ResourceId = "uistrings.json";

    private readonly Dictionary<string, string> _entries;

    private UiStringCatalog(Dictionary<string, string> entries) => _entries = entries;

    public IReadOnlyDictionary<string, string> Entries => _entries;

    /// <summary>The text for a key, or empty when absent. Never the key itself: a raw key on
    /// screen is the failure this catalog exists to remove.</summary>
    public string Get(string key) => _entries.TryGetValue(key, out string v) ? v : "";

    public bool TryGet(string key, out string value) => _entries.TryGetValue(key, out value);

    public static UiStringCatalog FromJson(string json) {
        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();
        return new UiStringCatalog(parsed);
    }

    /// <summary>Per-entry override, later-source-wins — the rule <c>ContentRegistry.Merge</c>
    /// uses, so a translation can replace part of the catalog without restating all of it.</summary>
    public UiStringCatalog Merge(UiStringCatalog over) {
        var merged = new Dictionary<string, string>(_entries);
        if (over != null) {
            foreach (KeyValuePair<string, string> kv in over._entries) {
                merged[kv.Key] = kv.Value;
            }
        }
        return new UiStringCatalog(merged);
    }

    private static UiStringCatalog _embedded;

    /// <summary>The catalog compiled into this assembly. One copy, nothing to hand-sync.</summary>
    public static UiStringCatalog Embedded {
        get {
            if (_embedded == null) {
                Assembly asm = typeof(UiStringCatalog).Assembly;
                string name = null;
                foreach (string candidate in asm.GetManifestResourceNames()) {
                    if (candidate.EndsWith(ResourceId, StringComparison.Ordinal)) {
                        name = candidate;
                    }
                }
                if (name == null) {
                    _embedded = new UiStringCatalog(new Dictionary<string, string>());
                } else {
                    using Stream s = asm.GetManifestResourceStream(name);
                    using var r = new StreamReader(s);
                    _embedded = FromJson(r.ReadToEnd());
                }
            }
            return _embedded;
        }
    }
}
