namespace ResourceExtraction.Extractors.Exe;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Reads strings out of a DOS executable image by CONTENT, never by absolute offset. KRONDOR.EXE is
/// an overlaid MZ binary whose linear addresses are not file offsets, and the 1.00 floppy and 1.02 CD
/// builds differ — anchoring on content sidesteps both, and turns a version mismatch into a loud,
/// named failure instead of a silent garbage read.
/// </summary>
public static class ExeStringReader {
    /// <summary>A NUL-padded fixed-stride table, located by its first entry.</summary>
    public static IReadOnlyList<string> ReadTable(byte[] exe, string anchor, int stride, int count) {
        // Guard against invalid parameters before anchoring. A stride of zero would cause
        // the bounds check to degenerate (start > exe.Length is always false once found),
        // allowing ReadNulTerminated to be called with max=0, which silently returns empty
        // strings for every entry instead of throwing. This is exactly the failure mode we
        // exist to prevent, so reject it loudly instead.
        if (stride <= 0 || count < 0) {
            throw new InvalidDataException(
                $"EXE string table '{anchor}': stride must be > 0 (got {stride}), count must be >= 0 (got {count}).");
        }
        int start = FindEntry(exe, anchor, 0, stride);
        if (start < 0) {
            throw new InvalidDataException(
                $"EXE string table anchor '{anchor}' not found. The executable is not the expected build.");
        }
        if (start + stride * count > exe.Length) {
            throw new InvalidDataException(
                $"EXE string table '{anchor}' (stride {stride} x {count}) runs past the end of the image.");
        }
        var result = new List<string>(count);
        for (int i = 0; i < count; i++) {
            result.Add(ReadNulTerminated(exe, start + i * stride, stride));
        }
        return result;
    }

    /// <summary>
    /// One individually-referenced string, selected by 0-based <paramref name="occurrence"/>.
    ///
    /// <para><paramref name="expectedCount"/> is how many times the manifest's declarations account
    /// for this text. Spec §6 requires failing loudly when a string is "found more often than
    /// declared", so the whole image is scanned even after the wanted occurrence is located: an
    /// undeclared extra copy means a call site nobody has keyed, which is a translation hole that
    /// would otherwise ship silently. Stopping early would make the check unenforceable, so the cost
    /// of the full scan is the point, not an oversight.</para>
    /// </summary>
    public static string ReadSingle(byte[] exe, string text, int occurrence, int expectedCount) {
        byte[] needle = Encoding.ASCII.GetBytes(text);
        int seen = 0;
        for (int i = 0; i + needle.Length < exe.Length; i++) {
            if (!Matches(exe, i, needle) || exe[i + needle.Length] != 0) {
                continue;
            }
            // A match must be a WHOLE string, not a tail sharing someone else's terminator.
            // "Damage:" otherwise matches inside "Base Damage:\0", "%ld silver" inside
            // "%ld gold %ld silver\0", "%s%s%s%s" inside "%s%s%s%s%s%s%s\0" — the trailing NUL alone
            // does not distinguish them. In a C string pool every string is preceded by the previous
            // one's terminator, so requiring a leading NUL (or the very start of the image) is the
            // mirror of the trailing-NUL rule FindEntry already applies to table anchors. Without
            // it, `occurrence` silently numbered phantom entries: "Damage:" occurrence 0 resolved to
            // the tail of "Base Damage:" rather than to the combat panel's own literal.
            if (i > 0 && exe[i - 1] != 0) {
                continue;
            }
            seen++;
        }
        if (seen > expectedCount) {
            throw new InvalidDataException(
                $"EXE string '{text}' found {seen} times but only {expectedCount} declaration(s) " +
                "claim it. Every occurrence is a separate call site and needs its own key — declare " +
                "the extra occurrence(s) in ExeStringManifest.Singles, or exclude the text with a " +
                "reason in docs/re-notes/exe-display-strings.md.");
        }
        if (occurrence < 0 || occurrence >= seen) {
            throw new InvalidDataException(
                $"EXE string '{text}' occurrence {occurrence} not found (found {seen}).");
        }
        return text;
    }

    // An anchor must be a whole NUL-terminated entry, so "Health" cannot match inside
    // "HealthPotion", and it must sit at a position the stride can walk from.
    private static int FindEntry(byte[] exe, string anchor, int from, int stride) {
        byte[] needle = Encoding.ASCII.GetBytes(anchor);
        for (int i = from; i + needle.Length < exe.Length; i++) {
            if (Matches(exe, i, needle) && exe[i + needle.Length] == 0) {
                return i;
            }
        }
        return -1;
    }

    private static bool Matches(byte[] exe, int at, byte[] needle) {
        if (at + needle.Length > exe.Length) {
            return false;
        }
        for (int j = 0; j < needle.Length; j++) {
            if (exe[at + j] != needle[j]) {
                return false;
            }
        }
        return true;
    }

    private static string ReadNulTerminated(byte[] exe, int at, int max) {
        int len = 0;
        while (len < max && at + len < exe.Length && exe[at + len] != 0) {
            len++;
        }
        return Encoding.ASCII.GetString(exe, at, len);
    }
}
