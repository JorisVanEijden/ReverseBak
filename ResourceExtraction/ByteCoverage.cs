namespace ResourceExtraction;

using System;
using System.Collections.Generic;

/// <summary>
/// Accumulates the half-open byte ranges a <see cref="SaveGameWriter"/> authors from our own model,
/// merging overlaps/adjacencies so bytes are never double-counted. Used to measure how much of the
/// save body we author vs. pass through unchanged (goal: passthrough -> 0).
/// </summary>
public sealed class ByteCoverage {
    private readonly List<(int Start, int End)> _ranges = new(); // half-open [Start, End)

    public void Add(int offset, int length) {
        if (length <= 0) {
            return;
        }
        _ranges.Add((offset, offset + length));
    }

    private List<(int Start, int End)> Merged() {
        var sorted = new List<(int Start, int End)>(_ranges);
        sorted.Sort((a, b) => a.Start.CompareTo(b.Start));
        var merged = new List<(int Start, int End)>();
        foreach (var r in sorted) {
            if (merged.Count > 0 && r.Start <= merged[^1].End) {
                merged[^1] = (merged[^1].Start, Math.Max(merged[^1].End, r.End));
            } else {
                merged.Add(r);
            }
        }
        return merged;
    }

    public int AuthoredBytes {
        get {
            int total = 0;
            foreach (var (start, end) in Merged()) {
                total += end - start;
            }
            return total;
        }
    }

    public IReadOnlyList<(int Offset, int Length)> Ranges {
        get {
            var result = new List<(int Offset, int Length)>();
            foreach (var (start, end) in Merged()) {
                result.Add((start, end - start));
            }
            return result;
        }
    }
}
