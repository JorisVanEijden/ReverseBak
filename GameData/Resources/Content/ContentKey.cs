namespace GameData.Resources.Content;

using System;
using System.Globalization;

/// <summary>Content identity keys: canonical <c>base:&lt;catalog&gt;:&lt;index&gt;</c> for original
/// game content, <c>&lt;mod&gt;:&lt;key&gt;</c> for mod-authored content. See the additive content
/// registry design (docs/superpowers/specs/2026-07-21-additive-content-registry-design.md §5.1).</summary>
public static class ContentKey {
    public const string BaseNamespace = "base";

    public static string ForBase(string catalog, int index) => $"{BaseNamespace}:{catalog}:{index}";

    public static string ForMod(string mod, string key) => $"{mod}:{key}";

    public static string NamespaceOf(string? key) {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        int i = key!.IndexOf(':');
        return i <= 0 ? string.Empty : key.Substring(0, i);
    }

    public static bool IsValid(string? key) {
        if (string.IsNullOrEmpty(key)) return false;
        int i = key!.IndexOf(':');
        return i > 0 && i < key.Length - 1;
    }

    public static bool TryParseBase(string? key, string catalog, out int index) {
        index = 0;
        if (key is null) return false;
        string prefix = $"{BaseNamespace}:{catalog}:";
        if (!key.StartsWith(prefix, StringComparison.Ordinal)) return false;
        return int.TryParse(key.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out index);
    }
}
