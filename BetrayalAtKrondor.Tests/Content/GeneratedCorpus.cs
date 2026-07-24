namespace BetrayalAtKrondor.Tests.Content;

using System;
using System.IO;

/// <summary>Shared locator for the committed <c>generated/</c> corpus, used by the reference-integrity
/// tests (see docs/re-notes/reference-inventory.md). Walks up from the test-assembly base directory
/// looking for a <c>generated/</c> folder that contains the required sub-paths. Returns <c>null</c>
/// when absent (e.g. a CI runner without game data) so tests can skip rather than fail — the same
/// skip-if-absent contract the game-data tests use.</summary>
internal static class GeneratedCorpus {
    /// <summary>Finds the <c>generated/</c> directory whose immediate children include every entry in
    /// <paramref name="requiredEntries"/> (files or directories). Returns the absolute path, or
    /// <c>null</c> if no ancestor has a matching <c>generated/</c>.</summary>
    public static string? FindDir(params string[] requiredEntries) {
        DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null) {
            string candidate = Path.Combine(dir.FullName, "generated");
            if (Directory.Exists(candidate) && HasAll(candidate, requiredEntries)) {
                return candidate;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private static bool HasAll(string root, string[] entries) {
        foreach (string e in entries) {
            string p = Path.Combine(root, e);
            if (!Directory.Exists(p) && !File.Exists(p)) {
                return false;
            }
        }
        return true;
    }
}
